"""Loopback-only, single-process visual input bridge. No simulator APIs."""
import ctypes as c
from ctypes import wintypes as w
import io, json, secrets, sys, time, atexit, signal
from datetime import datetime, timezone
from http.server import HTTPServer, BaseHTTPRequestHandler
from PIL import Image

PID = int(sys.argv[1])
TOKEN = sys.argv[2] if len(sys.argv)>2 else secrets.token_urlsafe(24)
# User-authorized deadline. This guard owns only the test transport and
# never reads simulator state. Preserve the original PID-pinned DPI/occlusion
# checks and reject new UI work after the cutoff even if a caller is stale.
DEADLINE = datetime(2026, 10, 23, 11, 0, 0, tzinfo=timezone.utc)
def require_before_deadline():
    if datetime.now(timezone.utc) >= DEADLINE:
        raise RuntimeError('User cutoff reached; stop project work and perform backup closeout only')

u, g = c.windll.user32, c.windll.gdi32
# All capture/ClientToScreen/SetCursorPos work on this same HTTP thread.
# Per-monitor-v2 removes DPI virtualization; never mix logical capture pixels
# with physical cursor coordinates or apply a second scale conversion.
u.SetThreadDpiAwarenessContext.argtypes=[c.c_void_p]
u.SetThreadDpiAwarenessContext.restype=c.c_void_p
u.GetThreadDpiAwarenessContext.restype=c.c_void_p
u.GetAwarenessFromDpiAwarenessContext.argtypes=[c.c_void_p]
u.GetAwarenessFromDpiAwarenessContext.restype=c.c_int
_previous_dpi_context=u.SetThreadDpiAwarenessContext(c.c_void_p(-4))
if not _previous_dpi_context or u.GetAwarenessFromDpiAwarenessContext(u.GetThreadDpiAwarenessContext())!=2:
    raise RuntimeError('Per-monitor DPI-aware input/capture context unavailable; no bridge started')
u.IsIconic.argtypes=[w.HWND]
u.GetWindowRect.argtypes=[w.HWND,c.POINTER(w.RECT)]
u.SetWindowPos.argtypes=[w.HWND,w.HWND,c.c_int,c.c_int,c.c_int,c.c_int,w.UINT]
u.SetWindowPos.restype=w.BOOL
for name in ('GetDC',):
    getattr(u, name).restype = w.HDC
g.CreateCompatibleDC.restype = w.HDC
g.CreateCompatibleBitmap.restype = w.HBITMAP
g.SelectObject.restype = w.HANDLE
g.SelectObject.argtypes = [w.HDC,w.HANDLE]
g.CreateCompatibleDC.argtypes = [w.HDC]
g.CreateCompatibleBitmap.argtypes = [w.HDC,c.c_int,c.c_int]
g.DeleteObject.argtypes = [w.HANDLE]
g.DeleteDC.argtypes = [w.HDC]
g.GetDIBits.argtypes = [w.HDC,w.HBITMAP,w.UINT,w.UINT,c.c_void_p,c.c_void_p,w.UINT]
g.BitBlt.argtypes=[w.HDC,c.c_int,c.c_int,c.c_int,c.c_int,w.HDC,c.c_int,c.c_int,w.DWORD]
g.BitBlt.restype=w.BOOL
u.GetSystemMetrics.argtypes=[c.c_int]
u.GetSystemMetrics.restype=c.c_int
u.IsWindowVisible.argtypes=[w.HWND]
u.ReleaseDC.argtypes = [w.HWND,w.HDC]
u.GetForegroundWindow.restype = w.HWND
u.GetWindowThreadProcessId.argtypes = [w.HWND,c.POINTER(w.DWORD)]
u.SetForegroundWindow.argtypes = [w.HWND]
u.SetForegroundWindow.restype = w.BOOL
u.GetWindowThreadProcessId.restype = w.DWORD
u.AttachThreadInput.argtypes = [w.DWORD,w.DWORD,w.BOOL]
u.AttachThreadInput.restype = w.BOOL
k = c.windll.kernel32
k.GetCurrentThreadId.restype = w.DWORD
u.GetClientRect.argtypes = [w.HWND,c.POINTER(w.RECT)]
u.ClientToScreen.argtypes = [w.HWND,c.POINTER(w.POINT)]

# Use the complete Win32 INPUT union: omitting MOUSEINPUT would make sizeof
# INPUT too small on x64 even when only KEYBDINPUT packets are sent.
ULONG_PTR = c.c_size_t
class MOUSEINPUT(c.Structure):
    _fields_ = [('dx',w.LONG),('dy',w.LONG),('mouseData',w.DWORD),
                ('dwFlags',w.DWORD),('time',w.DWORD),('dwExtraInfo',ULONG_PTR)]
class KEYBDINPUT(c.Structure):
    _fields_ = [('wVk',w.WORD),('wScan',w.WORD),('dwFlags',w.DWORD),
                ('time',w.DWORD),('dwExtraInfo',ULONG_PTR)]
class HARDWAREINPUT(c.Structure):
    _fields_ = [('uMsg',w.DWORD),('wParamL',w.WORD),('wParamH',w.WORD)]
class INPUTUNION(c.Union):
    _fields_ = [('mi',MOUSEINPUT),('ki',KEYBDINPUT),('hi',HARDWAREINPUT)]
class INPUT(c.Structure):
    _anonymous_ = ('payload',)
    _fields_ = [('type',w.DWORD),('payload',INPUTUNION)]

assert c.sizeof(INPUT) == (40 if c.sizeof(c.c_void_p)==8 else 28)
_send_input = c.WinDLL('user32',use_last_error=True).SendInput
_send_input.argtypes = [w.UINT,c.POINTER(INPUT),c.c_int]
_send_input.restype = w.UINT
KEYS = {'Escape':27,'Return':13,'Tab':9,'Space':32,'Down':40,'Up':38,
        'Home':36,'End':35,'PageDown':34,'PageUp':33,'Left':37,'Right':39,
        'BackSpace':8,'Delete':46}
CHORDS = {'Ctrl+A':(17,65)}
BUTTON_FLAGS = {'left':(0x0002,0x0004),'right':(0x0008,0x0010),
                'middle':(0x0020,0x0040)}
HELD_BUTTONS = set()

def release_held_buttons():
    # Up-only cleanup of buttons this bridge depressed. Never manufacture a
    # down/click or release buttons pressed outside the bridge. Releases clear
    # global native mouse state even when focus was lost; no new gesture begins.
    for button in tuple(HELD_BUTTONS):
        u.mouse_event(BUTTON_FLAGS[button][1],0,0,0,0)
        HELD_BUTTONS.discard(button)

atexit.register(release_held_buttons)


def validate_input(data):
    """Validate the complete request before activating or typing into a window."""
    if not isinstance(data,dict):raise ValueError('Input must be a JSON object')
    modes = [name for name in ('text','key','keys') if name in data]
    if 'x' in data or 'y' in data:modes.append('mouse')
    if len(modes)!=1:raise ValueError('Choose exactly one input mode')
    mode=modes[0]
    if mode=='text':
        if set(data)!={'text'} or not isinstance(data['text'],str):
            raise ValueError('Text input requires only a text string')
        if len(data['text'])>4096:raise ValueError('At most4096 text characters')
        data['text'].encode('utf-16-le')  # Reject unpaired Unicode surrogates.
    elif mode in ('key','keys'):
        if set(data)!={mode}:raise ValueError('Unexpected key fields')
        sequence=[data['key']] if mode=='key' else data['keys']
        if not isinstance(sequence,list) or not 1<=len(sequence)<=100:
            raise ValueError('Use1 to100 whitelisted keys')
        if any(not isinstance(key,str) or key not in KEYS and key not in CHORDS
               for key in sequence):raise ValueError('Unsupported key or chord')
    else:
        if set(data)-{'x','y','phase','delta','button'}:raise ValueError('Unexpected mouse fields')
        if any(type(data.get(axis)) is not int for axis in ('x','y')):
            raise ValueError('Mouse coordinates must be integers')
        phase=data.get('phase','click')
        if phase not in ('click','down','up','wheel','move'):raise ValueError('Unsupported mouse phase')
        if 'button' in data and phase not in ('down','up'):
            raise ValueError('Explicit button requires down/up phase')
        if not isinstance(data.get('button','left'),str) or data.get('button','left') not in BUTTON_FLAGS:
            raise ValueError('Button must be left, right or middle')
        if 'delta' in data and phase!='wheel':raise ValueError('Delta requires wheel phase')
        if phase=='wheel' and (type(data.get('delta',-120)) is not int
                              or not -1200<=data.get('delta',-120)<=1200):
            raise ValueError('Wheel delta must be an integer from-1200 to1200')
    return mode

def activate_pinned_window(hwnd):
    """Temporarily join OS input queues; never send input to a foreign app."""
    target_pid=w.DWORD()
    target_thread=u.GetWindowThreadProcessId(hwnd,c.byref(target_pid))
    if target_pid.value!=PID or not target_thread:
        raise RuntimeError('Activation target is not the pinned app')
    foreground=u.GetForegroundWindow()
    foreground_pid=w.DWORD()
    foreground_thread=u.GetWindowThreadProcessId(foreground,c.byref(foreground_pid))
    if foreground_pid.value==PID:return  # preserve app-owned dialog focus
    if HELD_BUTTONS:
        release_held_buttons()
        raise RuntimeError('Focus lost during drag; held buttons released; retry after verifying app')
    current=k.GetCurrentThreadId()
    attached=[]
    try:
        for thread in dict.fromkeys((foreground_thread,target_thread)):
            if thread and thread!=current and u.AttachThreadInput(current,thread,True):
                attached.append(thread)
        # Only the validated pinned HWND is ever activated.
        u.SetForegroundWindow(hwnd)
    finally:
        for thread in reversed(attached):u.AttachThreadInput(current,thread,False)
    time.sleep(.15)

def require_pinned_focus():
    """SendInput is foreground input; refuse to type if activation did not work."""
    focused=u.GetForegroundWindow()
    focused_pid=w.DWORD()
    u.GetWindowThreadProcessId(focused,c.byref(focused_pid))
    if focused_pid.value!=PID:
        release_held_buttons()
        raise RuntimeError('Pinned app did not acquire foreground focus; no input sent')

def send_packets(packets):
    require_pinned_focus()
    if not packets:return
    array=(INPUT*len(packets))(*packets)
    c.set_last_error(0)
    delivered=_send_input(len(array),array,c.sizeof(INPUT))
    if delivered!=len(array):
        raise RuntimeError(f'SendInput delivered{delivered}/{len(array)} packets; '
                           f'Windows error{c.get_last_error()}. Check focus and app integrity level.')

def keyboard_packet(vk=0,scan=0,flags=0):
    packet=INPUT();packet.type=1
    packet.ki=KEYBDINPUT(vk,scan,flags,0,0)
    return packet

def type_unicode(text):
    # KEYEVENTF_UNICODE uses UTF-16 code units, including surrogate pairs.
    raw=text.encode('utf-16-le')
    units=[int.from_bytes(raw[i:i+2],'little') for i in range(0,len(raw),2)]
    for offset in range(0,len(units),32):
        packets=[]
        for unit in units[offset:offset+32]:
            packets.extend((keyboard_packet(scan=unit,flags=4),
                            keyboard_packet(scan=unit,flags=4|2)))
        send_packets(packets)
        time.sleep(.02)

def press_key(key):
    if key in CHORDS:
        modifier,vk=CHORDS[key]
        try:
            send_packets([keyboard_packet(vk=modifier),keyboard_packet(vk=vk),
                          keyboard_packet(vk=vk,flags=2),keyboard_packet(vk=modifier,flags=2)])
        finally:
            # Ensure an incomplete chord cannot leave Ctrl held down.
            u.keybd_event(modifier,0,2,0)
    else:
        require_pinned_focus()
        vk=KEYS[key]
        u.keybd_event(vk,0,0,0);time.sleep(.025);u.keybd_event(vk,0,2,0)
    time.sleep(.045)

def window():
    found = []
    @c.WINFUNCTYPE(w.BOOL, w.HWND, w.LPARAM)
    def visit(hwnd, _):
        pid = w.DWORD()
        u.GetWindowThreadProcessId(hwnd, c.byref(pid))
        if pid.value == PID and u.IsWindowVisible(hwnd):
            rect=w.RECT();u.GetClientRect(hwnd,c.byref(rect))
            found.append((rect.right*rect.bottom,hwnd))
        return True
    u.EnumWindows(visit, 0)
    if not found:
        raise RuntimeError('Pinned test window unavailable')
    return max(found)[1]

def client_size():
    hwnd=window()
    rect=w.RECT();u.GetClientRect(hwnd,c.byref(rect))
    return [rect.right,rect.bottom]

def prepare_native_canvas(resize=False):
    hwnd=window()
    if u.IsIconic(hwnd):raise RuntimeError('Pinned app is minimized; restore it before transport startup')
    rect=w.RECT();u.GetClientRect(hwnd,c.byref(rect))
    if resize and [rect.right,rect.bottom]!=[1021,660]:
        outer=w.RECT();u.GetWindowRect(hwnd,c.byref(outer))
        width=outer.right-outer.left+1021-rect.right
        height=outer.bottom-outer.top+660-rect.bottom
        # Frame geometry only, exact pinned HWND, no activation or app API.
        if not u.SetWindowPos(hwnd,None,outer.left,outer.top,width,height,0x0004|0x0010):
            raise RuntimeError('Pinned native client resize failed')
        time.sleep(.15)
    if client_size()!=[1021,660]:
        raise RuntimeError('Native physical client must be1021x660; use explicit --resize-client1021x660 or root native setup')
    return hwnd

def require_unoccluded_client(hwnd,left,top,right,bottom):
    # Screen capture must never silently substitute another app's pixels.
    # Require pinned foreground and reject visible foreign top-level windows
    # above the pinned window whose bounding rectangle touches its client.
    require_pinned_focus()
    vx,vy=u.GetSystemMetrics(76),u.GetSystemMetrics(77)
    vw,vh=u.GetSystemMetrics(78),u.GetSystemMetrics(79)
    if left<vx or top<vy or right>vx+vw or bottom>vy+vh:
        raise RuntimeError('Pinned client is partly outside virtual screen; no capture')
    reached=False;blockers=[]
    @c.WINFUNCTYPE(w.BOOL,w.HWND,w.LPARAM)
    def inspect(candidate,_):
        nonlocal reached
        if candidate==hwnd:
            reached=True
            return False
        if not u.IsWindowVisible(candidate) or u.IsIconic(candidate):return True
        pid=w.DWORD();u.GetWindowThreadProcessId(candidate,c.byref(pid))
        if pid.value==PID:return True  # app-owned menus/dialogs are intended UI
        bounds=w.RECT()
        if u.GetWindowRect(candidate,c.byref(bounds)):
            if bounds.left<right and bounds.right>left and bounds.top<bottom and bounds.bottom>top:
                blockers.append(int(candidate))
        return True
    u.EnumWindows(inspect,0)
    if not reached or blockers:
        raise RuntimeError('Pinned client occlusion/stack check failed; no capture. Foreign HWNDs='+str(blockers))

def capture():
    require_before_deadline()
    hwnd = prepare_native_canvas()
    activate_pinned_window(hwnd)
    require_pinned_focus()
    rect = w.RECT()
    u.GetClientRect(hwnd, c.byref(rect))
    width, height = rect.right, rect.bottom
    origin=w.POINT(0,0);u.ClientToScreen(hwnd,c.byref(origin))
    require_unoccluded_client(hwnd,origin.x,origin.y,origin.x+width,origin.y+height)
    dc = u.GetDC(None)  # screen DC uses actual compositor physical pixels
    mem = g.CreateCompatibleDC(dc)
    bitmap = g.CreateCompatibleBitmap(dc, width, height)
    old = g.SelectObject(mem, bitmap)
    try:
        if not g.BitBlt(mem,0,0,width,height,dc,origin.x,origin.y,0x00CC0020|0x40000000):
            raise RuntimeError('Native screen BitBlt failed')
        require_pinned_focus()
        class BI(c.Structure):
            _fields_ = [('size',w.DWORD),('width',w.LONG),('height',w.LONG),('planes',w.WORD),('bits',w.WORD),('compression',w.DWORD),('image_size',w.DWORD),('xp',w.LONG),('yp',w.LONG),('used',w.DWORD),('important',w.DWORD)]
        info = BI(40,width,-height,1,32,0,0,0,0,0,0)
        pixels = c.create_string_buffer(width*height*4)
        if not g.GetDIBits(mem,bitmap,0,height,pixels,c.byref(info),0):
            raise RuntimeError('Capture failed')
        img = Image.frombuffer('RGB',(width,height),pixels,'raw','BGRX',0,1)
        out = io.BytesIO()
        img.save(out,format='PNG')
        return out.getvalue()
    finally:
        g.SelectObject(mem,old)
        g.DeleteObject(bitmap)
        g.DeleteDC(mem)
        u.ReleaseDC(None,dc)

PAGE = '''<!doctype html><meta charset="utf-8"><title>RungProof Screen Bridge</title>
<style>body{margin:0;background:#222;color:white}img{display:block;width:100%;max-width:1600px}button{margin:4px}</style>
<div>Real RungProof test window — click image to send OS mouse input. <button onclick="refresh()">Refresh screen</button><button onclick="key('Escape')">Escape</button></div>
<img id="screen" alt="Live RungProof window" draggable="false">
<script>
const token=location.pathname; const img=document.getElementById('screen');
function refresh(){img.src=token+'/screen?t='+Date.now()}
async function send(data){await fetch(token+'/input',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});setTimeout(refresh,250)}
img.onclick=e=>{let r=img.getBoundingClientRect();send({x:Math.round((e.clientX-r.left)*img.naturalWidth/r.width),y:Math.round((e.clientY-r.top)*img.naturalHeight/r.height)})};
function key(value){send({key:value})} refresh();
</script>'''

class Handler(BaseHTTPRequestHandler):
    def log_message(self,*args): pass
    def reply(self,data,kind='text/html',status=200):
        self.send_response(status)
        self.send_header('Content-Type',kind)
        self.send_header('Cache-Control','no-store')
        self.end_headers()
        self.wfile.write(data)
    def do_GET(self):
        if self.path == '/'+TOKEN+'/health':
            import os
            self.reply(json.dumps({'version':11,'deadline_utc':DEADLINE.isoformat(),'activation_mode':'pinned-only-temporary-AttachThreadInput-finally-detach','pid':os.getpid(),'app_pid':PID,
                'keys':list(KEYS),'chords':list(CHORDS),'text':True,
                'mouse_buttons':list(BUTTON_FLAGS),'mouse_move':True,
                'coordinate_mode':'native-physical-pixels-per-monitor-v2',
                'capture_mode':'foreground-screenDC-BitBlt-unoccluded-client',
                'thread_dpi_awareness':u.GetAwarenessFromDpiAwarenessContext(u.GetThreadDpiAwarenessContext()),
                'native_client':client_size(),
                'mouse_phases':['click','down','up','wheel','move'],
                'text_max_characters':4096,'input_struct_bytes':c.sizeof(INPUT)}).encode(),'application/json')
        elif self.path.split('?')[0] == '/'+TOKEN:
            self.reply(PAGE.encode())
        elif self.path.startswith('/'+TOKEN+'/screen?'):
            try: self.reply(capture(),'image/png')
            except Exception as exc: self.reply(str(exc).encode(),'text/plain',500)
        else: self.reply(b'Not found','text/plain',404)
    def do_POST(self):
        try:
            self.handle_input()
        except Exception as exc:
            release_held_buttons()
            import traceback
            with open(__file__+'.errors.log','a') as log:log.write(traceback.format_exc())
            self.reply(str(exc).encode(),'text/plain',500)
    def handle_input(self):
        if self.path != '/'+TOKEN+'/input':
            return self.reply(b'Not found','text/plain',404)
        length=int(self.headers.get('Content-Length',0))
        if not 0<length<=65536:raise ValueError('Input body must be1 to65536 bytes')
        data=json.loads(self.rfile.read(length))
        mode=validate_input(data)
        require_before_deadline()
        hwnd=prepare_native_canvas()
        activate_pinned_window(hwnd)
        require_pinned_focus()
        if HELD_BUTTONS and mode!='mouse':
            raise ValueError('Finish pointer gesture before keyboard/text input')
        if mode=='text':
            type_unicode(data['text'])
        elif mode in ('key','keys'):
            sequence=[data['key']] if mode=='key' else data['keys']
            for key in sequence:press_key(key)
        else:
            x,y=int(data['x']),int(data['y'])
            rect=w.RECT();u.GetClientRect(hwnd,c.byref(rect))
            if not (0<=x<rect.right and 0<=y<rect.bottom):
                release_held_buttons()
                return self.reply(b'Out of bounds','text/plain',400)
            phase=data.get('phase','click')
            button=data.get('button','left')
            if phase=='down' and (button in HELD_BUTTONS or HELD_BUTTONS):
                raise ValueError('Only one bridge-held mouse button at a time')
            if phase=='up' and button not in HELD_BUTTONS:
                raise ValueError('Cannot release a button not held by this bridge')
            if HELD_BUTTONS and phase in ('click','wheel'):
                raise ValueError('Finish drag before click/wheel')
            require_pinned_focus()
            point=w.POINT(x,y);u.ClientToScreen(hwnd,c.byref(point))
            u.SetCursorPos(point.x,point.y)
            time.sleep(.05)
            require_pinned_focus()
            if phase in ('click','down'):
                HELD_BUTTONS.add(button)
                u.mouse_event(BUTTON_FLAGS[button][0],0,0,0,0)
            if phase=='click':time.sleep(.07)
            if phase in ('click','up'):
                require_pinned_focus()
                u.mouse_event(BUTTON_FLAGS[button][1],0,0,0,0)
                HELD_BUTTONS.discard(button)
            if phase=='wheel':u.mouse_event(0x0800,0,0,int(data.get('delta',-120)),0)
            # phase move only sets the native cursor, preserving held drag state.
        self.reply(b'Input delivered; verify visible result','text/plain')

if len(sys.argv)>5 or (len(sys.argv)==5 and sys.argv[4]!='--resize-client1021x660'):
    raise RuntimeError('Only optional startup flag is --resize-client1021x660')
require_before_deadline()
prepare_native_canvas(resize=len(sys.argv)==5)
server=HTTPServer(('127.0.0.1',int(sys.argv[3]) if len(sys.argv)>3 else 0),Handler)
print(f'http://127.0.0.1:{server.server_port}/{TOKEN}',flush=True)
def terminate_server(signum, frame):
    raise SystemExit(0)
signal.signal(signal.SIGTERM,terminate_server)
try:
    server.serve_forever()
finally:
    release_held_buttons()
    server.server_close()

