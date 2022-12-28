# MhLabs.AwsSignedHttpClient

## Pushing a new version
Set the `Version` number in the <a href="https://github.com/mhlabs/MhLabs.AwsSignedHttpClient/blob/master/MhLabs.AwsSignedHttpClient/MhLabs.AwsSignedHttpClient.csproj"> .csproj-file</a> before pushing. If an existing version is pushed the <a href="https://github.com/mhlabs/MhLabs.AwsSignedHttpClient/actions">build will fail</a>.

## Publish pre-release packages on branches to allow us to test the package without merging to master
1. Create a new branch
2. Update `Version` number and add `-beta` postfix (can have .1, .2 etc. at the end)
3. Make any required changes updating the version as you go
4. Test beta package in solution that uses package
5. Create PR and get it reviewed
6. Check if there are any changes on the branch you're merging into. If there are you need to rebase those changes into yours and check that it still builds
7. As the final thing before merging update version number and remove post fix
