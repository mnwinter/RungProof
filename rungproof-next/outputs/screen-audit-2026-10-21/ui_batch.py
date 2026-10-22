"""Run explicitly grounded screen coordinates; save labeled checkpoint evidence."""
import hashlib,io,json,sys,time,urllib.request
from pathlib import Path
from qa_input_transport import post_input,require_canvas
from PIL import Image,ImageDraw
root=Path(__file__).parent
url=sys.argv[1]
plan=json.loads((root/sys.argv[2]).read_text())
require_canvas(url,{'helper':'ui_batch','plan':sys.argv[2],'stage':'before_any_input'})
frames=[]
for step in plan:
    event={'utc_ns':time.time_ns(),'plan':sys.argv[2],'step':step}
    if 'key' in step:
        data=json.dumps({'key':step['key']}).encode()
        post_input(url,data,context={'helper':'ui_batch','plan':sys.argv[2],'step':step})
    if 'text' in step:
        data=json.dumps({'text':step['text']}).encode()
        post_input(url,data,context={'helper':'ui_batch','plan':sys.argv[2],'step':step})
    if 'wheel' in step:
        x,y,delta=step['wheel']
        data=json.dumps({'x':x,'y':y,'phase':'wheel','delta':delta}).encode()
        post_input(url,data,context={'helper':'ui_batch','plan':sys.argv[2],'step':step})
    if 'keys' in step:
        data=json.dumps({'keys':step['keys']}).encode()
        post_input(url,data,context={'helper':'ui_batch','plan':sys.argv[2],'step':step})
    if 'click' in step:
        x,y=step['click']
        data=json.dumps({'x':x,'y':y}).encode()
        post_input(url,data,context={'helper':'ui_batch','plan':sys.argv[2],'step':step})
    for phase in ('down','up'):
        if phase in step:
            x,y=step[phase]
            data=json.dumps({'x':x,'y':y,'phase':phase}).encode()
            post_input(url,data,context={'helper':'ui_batch','plan':sys.argv[2],'step':step})
    time.sleep(step.get('wait',.3))
    if 'capture' in step:
        name=step['capture']
        raw=urllib.request.urlopen(url+'/screen?t='+str(time.time_ns())).read()
        (root/(name+'.png')).write_bytes(raw)
        event['capture_sha256']=hashlib.sha256(raw).hexdigest()
        img=Image.open(io.BytesIO(raw)).convert('RGB')
        img.thumbnail((800,450))
        frame=Image.new('RGB',(800,475),'white')
        ImageDraw.Draw(frame).text((8,5),name,fill='black')
        frame.paste(img,(0,25));frames.append(frame)
    with (root/'ui-action-log.jsonl').open('a') as log:
        log.write(json.dumps(event)+'\n')
if not frames:
    print(json.dumps({'plan':sys.argv[2],'steps':len(plan),'captures':0,'contact_sheet':None,'status':'actions_completed_no_captures_requested'}))
else:
    sheet=Image.new('RGB',(1600,475*((len(frames)+1)//2)),'#ddd')
    for i,frame in enumerate(frames):sheet.paste(frame,((i%2)*800,(i//2)*475))
    out=root/(Path(sys.argv[2]).stem+'-checkpoints.jpg')
    sheet.save(out,quality=88)
    print(out)
