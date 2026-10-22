"""Retry only proven pre-delivery focus rejection; no native or simulator APIs."""
import ast,json,re,time,urllib.request,urllib.error
from pathlib import Path
ROOT=Path(__file__).parent
BRIDGE=ROOT/'screen_bridge_v6.py'
ERROR_LOG=ROOT/'screen_bridge_v6.py.errors.log'
_TRANSPORT_CONTEXT={}
EXPECTED=b'Pinned app did not acquire foreground focus; no input sent'

def initial_focus_line(bridge_path):
    tree=ast.parse(bridge_path.read_text(encoding='utf-8-sig'))
    handler=next(n for n in ast.walk(tree) if isinstance(n,ast.FunctionDef) and n.name=='handle_input')
    # Top-level focus check precedes dispatch. Nested/repeated packet checks do
    # not prove no delivery and cannot authorize client replay.
    return next(n.lineno for n in handler.body if isinstance(n,ast.Expr)
                and isinstance(n.value,ast.Call) and isinstance(n.value.func,ast.Name)
                and n.value.func.id=='require_pinned_focus')

def proven_initial_rejection(added,entry_line):
    return (added.count('Traceback (most recent call last):')==1
            and re.search(r'line '+str(entry_line)+r', in handle_input\s+require_pinned_focus\(\)',added) is not None
            and added.rstrip().endswith('RuntimeError: '+EXPECTED.decode()))

def post_input(url,payload,context=None):
    raw=payload if isinstance(payload,bytes) else json.dumps(payload).encode()
    event=json.loads(raw)
    if url not in _TRANSPORT_CONTEXT:
        health=json.loads(urllib.request.urlopen(url+'/health',timeout=30).read())
        if health.get('app_pid')!=44956 or health.get('version') not in (6,7,8,9):
            raise RuntimeError('Retry transport requires pinned968 and bridge6/7/8/9')
        bridge=ROOT/('screen_bridge_v'+str(health['version'])+'.py')
        _TRANSPORT_CONTEXT[url]=(bridge,ROOT/(bridge.name+'.errors.log'),health['pid'])
    bridge,error_log,bridge_pid=_TRANSPORT_CONTEXT[url]
    entry=initial_focus_line(bridge)
    for attempt in range(1,4):
        offset=error_log.stat().st_size if error_log.exists() else 0
        try:
            result=urllib.request.urlopen(urllib.request.Request(url+'/input',data=raw,headers={'Content-Type':'application/json'}),timeout=30).read()
        except urllib.error.HTTPError as exc:
            body=exc.read()
            # Only this exact500 can be considered. Unknown errors/timeouts and
            # unreadable/mixed tracebacks remain fatal; no uncertain replay.
            added=''
            if exc.code==500 and body==EXPECTED and error_log.exists():
                with error_log.open('rb') as log:log.seek(offset);added=log.read().decode('utf-8',errors='replace')
            proof=exc.code==500 and body==EXPECTED and proven_initial_rejection(added,entry)
            record={'utc_ns':time.time_ns(),'context':context,'input':event,'attempt':attempt,
                    'http_status':exc.code,'error_body':body.decode('utf-8',errors='replace'),
                    'initial_guard_trace_proven':proof,'bridge_initial_focus_line':entry,'bridge_file':bridge.name,'bridge_pid':bridge_pid,
                    'bridge_trace_append':added,'will_retry':proof and attempt<3}
            with (ROOT/'client-focus-retries.jsonl').open('a') as log:log.write(json.dumps(record)+'\n')
            if not proof or attempt==3:raise
            time.sleep(.25)
            continue
        return result


def require_canvas(url,context):
    """Read native pixels only; fail before coordinate input on changed canvas."""
    import hashlib,io
    from PIL import Image
    stamp=str(time.time_ns())
    raw=urllib.request.urlopen(url+'/screen?t='+stamp,timeout=30).read()
    with Image.open(io.BytesIO(raw)) as shot:size=list(shot.size)
    ok=size==[1600,900]
    path=ROOT/('qa-canvas-'+('verified-' if ok else 'MISMATCH-')+stamp+'.png')
    with path.open('xb') as capture:capture.write(raw)
    with (ROOT/'qa-canvas-checks.jsonl').open('a') as log:
        log.write(json.dumps({'utc_ns':time.time_ns(),'context':context,
            'image':path.name,'sha256':hashlib.sha256(raw).hexdigest(),
            'native_size':size,'required_size':[1600,900],'matched':ok})+'\n')
    if not ok:raise RuntimeError('Native canvas mismatch; no further coordinate input. Inspect '+str(path))
    return size
