import json
from pathlib import Path
base=Path('D:/RungProof-Backup-2026-10-22');state={
'priority':'User requested full Git backup, next-Codex skill, complete D-drive migration before laptop wipe. Do not resume GUI testing.',
'source':'C:/Users/matt.winter/Documents/RungProof','backup':str(base),'git_main':'89151c79','deadline_app':'41188c70',
'release_id':421391212,'release_tag':'backup-2026-10-22-laptop-migration','release_draft':True,'github_account':'mnwinter via per-command GH_TOKEN from existing gh login; global account unchanged, no token stored',
'verified':'verification-full.json PASS119398files/26349194104bytes; verification-native-userdata.json PASS144files; junctionrestore18NTFSrehearsalPASS',
'active_sessions':{'raw_image_upload':19466,'content_store_then_archive':23419},
'archives_done':'raw-images4x700m volumes2.45GB currentlyuploading; raw-images-compressed3volumes1.87GB localonly; evidence-records1.6MB and local-extras.98MB alreadyuploaded; local-toolchains19volumes13.56GBlocalonly replacedremotebycontentaddressedarchive',
'content_store':'101062toolchainpaths map38022uniqueSHAblobs6.324GiB vs16.609GiB. Buildscript23419 copying/verifyingD/public-toolchain-content thencreates50MBpartarchive rungproof-toolchain-content.7z.*',
'remaining':['Finishcontentstore/7z t/uploadcontentarchiveparts; verifyGitHubassetsizes+digest againstSHA256SUMS; publishdraftrelease','Testatomicrestore-toolchain-content helperwithsampleactualblobpairs; scriptindependentreviewfixedatomictemporarycopy','Commithelperfix+latestskilldocs+backuphelperrecords. ValidateSkillagain;pushmain;Drefreshchangedfiles/Gitmetadataandverifydelta;GitfsckD','FinalizeRESTORE-FIRST,archiveindex/exclusionmanifest,FINAL-BACKUP-STATUS;verifyDprivatecontextsnapshotandskillcopies;preserveC']}
(base/'BACKUP-PROGRESS.json').write_text(json.dumps(state,indent=2),encoding='utf8')
