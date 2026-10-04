# Build scripts
Set `UNITY` to your Editor exe, then:
```
"$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Builds/editmode.xml
"$UNITY" -batchmode -quit -projectPath . -executeMethod NocturneAnnex.Editor.BuildScript.BuildAndroidApk -logFile Builds/build.log
```
Release signing env vars: NA_KEYSTORE_PATH, NA_KEYSTORE_PASS, NA_KEY_ALIAS, NA_KEY_PASS (keep outside the repo).
Note: do not pass -quit together with -runTests (Unity exits before tests run).
