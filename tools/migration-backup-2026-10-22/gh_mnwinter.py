import os,subprocess,json,sys
args=sys.argv[1:]
token=subprocess.check_output(['gh','auth','token','--hostname','github.com','--user','mnwinter'],text=True).strip()
env=os.environ.copy();env['GH_TOKEN']=token;env['GH_HOST']='github.com'
login=subprocess.check_output(['gh','api','user','--jq','.login'],env=env,text=True).strip()
assert login=='mnwinter','Unexpected backup GitHub account'
if not args:print(json.dumps({'verified_account':login}));sys.exit()
result=subprocess.run(['gh']+args,env=env)
sys.exit(result.returncode)
