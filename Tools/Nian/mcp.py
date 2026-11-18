"""Minimal local Unity MCP client; no computer-use automation."""
import urllib.request,urllib.error,json,sys,pathlib
URL='http://127.0.0.1:8080/mcp'
SESSION=pathlib.Path('Temp/nian-mcp-session.txt')
def request(method,params=None,session=None,ident=1):
 h={'Accept':'application/json, text/event-stream','Content-Type':'application/json'}
 if session:h['Mcp-Session-Id']=session
 payload={'jsonrpc':'2.0','method':method}
 if ident is not None:payload['id']=ident
 if params is not None:payload['params']=params
 r=urllib.request.urlopen(urllib.request.Request(URL,data=json.dumps(payload).encode(),headers=h),timeout=180)
 raw=r.read().decode('utf-8');data=None
 for line in raw.splitlines():
  if line.startswith('data: '):data=json.loads(line[6:])
 if data is None and raw.strip():data=json.loads(raw)
 return data,r.headers.get('Mcp-Session-Id')
def initialize():
 r,s=request('initialize',{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'Codex','version':'1.0'}})
 SESSION.parent.mkdir(exist_ok=True);SESSION.write_text(s);request('notifications/initialized',{},s,ident=None)
def call(method,params):
 if not SESSION.exists():initialize()
 try:return request(method,params,SESSION.read_text())[0]
 except urllib.error.HTTPError as error:
  if error.code!=404:raise
  # The Unity MCP server may have restarted since the last editing session.
  initialize()
  return request(method,params,SESSION.read_text())[0]
if __name__=='__main__':
 sys.stdout.reconfigure(encoding='utf-8')
 print(json.dumps(call(sys.argv[1],json.loads(sys.argv[2]) if len(sys.argv)>2 else {}),ensure_ascii=False,indent=2))
