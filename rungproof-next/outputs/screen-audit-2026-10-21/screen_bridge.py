"""Loopback-only, single-process visual input bridge. No simulator APIs."""
import ctypes as c
from ctypes import wintypes as w
import io, json, secrets, sys, time
from http.server import HTTPServer, BaseHTTPRequestHandler
from PIL import Image

PID = int(sys.argv[1])
TOKEN = sys.argv[2] if len(sys.argv)>2 else secrets.token_urlsafe(24)
u, g = c.windll.user32, c.windll.gdi32
u.SetProcessDPIAware()
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
u.PrintWindow.argtypes = [w.HWND,w.HDC,w.UINT]
u.ReleaseDC.argtypes = [w.HWND,w.HDC]

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

def capture():
    hwnd = window()
    rect = w.RECT()
    u.GetClientRect(hwnd, c.byref(rect))
    width, height = rect.right, rect.bottom
    dc = u.GetDC(hwnd)
    mem = g.CreateCompatibleDC(dc)
    bitmap = g.CreateCompatibleBitmap(dc, width, height)
    old = g.SelectObject(mem, bitmap)
    try:
        if not u.PrintWindow(hwnd, mem, 3):
            raise RuntimeError('PrintWindow failed')
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
        u.ReleaseDC(hwnd,dc)

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
            self.reply(json.dumps({'version':4,'pid':os.getpid(),'app_pid':PID,'keys':['Escape','Return','Home','Down']}).encode(),'application/json')
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
            import traceback
            with open(__file__+'.errors.log','a') as log:log.write(traceback.format_exc())
            self.reply(str(exc).encode(),'text/plain',500)
    def handle_input(self):
        if self.path != '/'+TOKEN+'/input':
            return self.reply(b'Not found','text/plain',404)
        data=json.loads(self.rfile.read(min(int(self.headers.get('Content-Length',0)),4096)))
        hwnd=window()
        u.SetForegroundWindow(hwnd)
        time.sleep(.15)
        if 'key' in data or 'keys' in data:
            keys={'Escape':27,'Return':13,'Tab':9,'Space':32,'Down':40,'Up':38,'Home':36,'End':35,'PageDown':34,'PageUp':33,'Left':37,'Right':39}
            sequence=data.get('keys',[data.get('key')])
            if len(sequence)>100:raise ValueError('At most100 navigation keys')
            for key in sequence:
                vk=keys[key]
                u.keybd_event(vk,0,0,0);time.sleep(.025);u.keybd_event(vk,0,2,0);time.sleep(.045)
        else:
            x,y=int(data['x']),int(data['y'])
            rect=w.RECT();u.GetClientRect(hwnd,c.byref(rect))
            if not (0<=x<rect.right and 0<=y<rect.bottom):
                return self.reply(b'Out of bounds','text/plain',400)
            point=w.POINT(x,y);u.ClientToScreen(hwnd,c.byref(point))
            u.SetCursorPos(point.x,point.y)
            time.sleep(.05)
            phase=data.get('phase','click')
            if phase in ('click','down'):u.mouse_event(2,0,0,0,0)
            if phase=='click':time.sleep(.07)
            if phase in ('click','up'):u.mouse_event(4,0,0,0,0)
            if phase=='wheel':u.mouse_event(0x0800,0,0,int(data.get('delta',-120)),0)
        self.reply(b'Input delivered; verify visible result','text/plain')

server=HTTPServer(('127.0.0.1',int(sys.argv[3]) if len(sys.argv)>3 else 0),Handler)
print(f'http://127.0.0.1:{server.server_port}/{TOKEN}',flush=True)
server.serve_forever()
