////////////////////////////////////////////////////////////////////////////////////////
/// version 6.8.0.0
/// 
/// Changelog: Calc SHA256 sums
/// Ticket: https://github.com/dtgm/chocolatey-packages/issues/196
/// 

// REQUIRES:
// global vars: saveDir=corresponds to download location of installer file
// app vars: nopush, checksum64file=corresponds to 64 bit install file
// file vars: same as specified by chocopkgup

// get package variable 'cscript'
string varCScript = app.Variables.ReplaceAllInString("{cscript}");

// determine whether we run this by checking cscript exists AND is 1 or 2
if ((varCScript == "1") || (varCScript == "2")) {

// ketarin variables we pass for this script to use
string varAppname = app.Variables.ReplaceAllInString("{appname}");
string varVersion = app.Variables.ReplaceAllInString("{version}");
string varChocoPkgOutput = app.Variables.ReplaceAllInString("{chocoPkgOut}");
string varChecksum = app.Variables.ReplaceAllInString("{checksum}");
string varChecksumx64 = app.Variables.ReplaceAllInString("{checksumx64}");
string varChecksum64File = app.Variables.ReplaceAllInString("{checksum64file}");
// string varChecksum64basefile = app.Variables.ReplaceAllInString("{checksum64file:basefile}");
string varChecksum64ext = app.Variables.ReplaceAllInString("{checksum64file:ext}");
string varSaveDir = app.Variables.ReplaceAllInString("{saveDir}");

// custom variables used in this script
string saveFileName64 = String.Concat(varAppname, "_64_", varVersion, ".", varChecksum64ext);
string savePath64 = Path.Combine(varSaveDir, saveFileName64);
// equivalent to ketarin variable "{file}"
string savePath = app.PreviousLocation;
string pkgPath = Path.Combine(varChocoPkgOutput, varAppname, varVersion);
string fileNameNuspec = String.Concat(varAppname, ".nuspec");
string fileUriNuspec = Path.Combine(pkgPath, fileNameNuspec);
string fileNameNupkg = String.Concat(varAppname, ".", varVersion, ".nupkg");
string fileUriNupkg = Path.Combine(pkgPath, fileNameNupkg);

/* DEBUG
  MessageBox.Show(varSaveDir + System.Environment.NewLine
                  + saveFileName64 + System.Environment.NewLine
                  + savePath64);*/

// do not re-push package if package already created
DateTime today = DateTime.Today;
DateTime pkgCreateDate = File.GetCreationTime(pkgPath);
if (today > pkgCreateDate) {
	return;
}

// if package variable 'checksum' does not exist or is null
if (varChecksum == "{checksum}") {
  // calculate SHA256 from {url}  Note we are leveraging ketarin's downloaded copy
  System.IO.FileStream fileSha = new System.IO.FileStream(savePath, System.IO.FileMode.Open);
  System.Security.Cryptography.SHA256 sha256 = new System.Security.Cryptography.SHA256Managed();
  byte[] retValSha = sha256.ComputeHash(fileSha);
  fileSha.Close();

  // build string from byte value
  System.Text.StringBuilder sbSha = new System.Text.StringBuilder();
  for (int i = 0; i < retValSha.Length; i++) {
    sbSha.Append(retValSha[i].ToString("x2"));
  }
  
  // find $pkgPath -iname "*.nuspec" -o -iname "*.ps1" -exec sed -i 's/'$sbSha'/{checksum}/g' '{}' \;
  string replaceChecksum = sbSha.ToString();
  //MessageBox.Show(replaceChecksum);
  List<string> fileList = new List<string>(Directory.GetFiles(pkgPath, "*.ps1", SearchOption.AllDirectories));
  string[] filesNuspec = Directory.GetFiles(pkgPath, "*.nuspec", SearchOption.AllDirectories);
  fileList.AddRange(filesNuspec);
  string[] files = fileList.ToArray();
  foreach (string file in files) {
    try {
      string contents = File.ReadAllText(file);
      //MessageBox.Show(contents);
      contents = contents.Replace("{checksum}", replaceChecksum);
      //MessageBox.Show(contents);
      // Make files writable
      // File.SetAttributes(file, FileAttributes.Normal);
      File.WriteAllText(file, contents);
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
    }
  }
}

// only get checksum if checksumx64 does NOT exist and 'checksum64file' DOES exists  
if (varChecksumx64 == "{checksumx64}" && varChecksum64File != "{checksum64file}") {
  // TODO: verify and validate URI checksum64file points to a downloadable file
  
  // we must download the file to calculate checksum ... may as well save it
  System.Net.WebClient webClient = new System.Net.WebClient();
  webClient.DownloadFile(varChecksum64File, savePath64);

  // calculate SHA256 from file of url pointed to by 'checksum64file'
  System.IO.FileStream file64Sha = new System.IO.FileStream(savePath64, System.IO.FileMode.Open);
  System.Security.Cryptography.SHA256 sha256_64 = new System.Security.Cryptography.SHA256Managed();
  byte[] retVal64Sha = sha256_64.ComputeHash(file64Sha);
  file64Sha.Close();

  // build string from byte value
  System.Text.StringBuilder sb64Sha = new System.Text.StringBuilder();
  for (int i = 0; i < retVal64Sha.Length; i++) {
    sb64Sha.Append(retVal64Sha[i].ToString("x2"));
  }
  
  // find $pkgPath -iname "*.nuspec" -o -iname "*.ps1" -exec sed -i 's/'$sb64Sha'/{checksumx64}/g' '{}' \;
  // Note chocopkgup will strip 1 set of curly braces so {{checksum}} becomes {checksum}
  string replace64Checksum = sb64Sha.ToString();
  //MessageBox.Show(replace64Checksum);
  List<string> fileList = new List<string>(Directory.GetFiles(pkgPath, "*.ps1", SearchOption.AllDirectories));
  string[] filesNuspec = Directory.GetFiles(pkgPath, "*.nuspec", SearchOption.AllDirectories);
  fileList.AddRange(filesNuspec);
  string[] files = fileList.ToArray();
  foreach (string file in files) {
    try {
      string contents = File.ReadAllText(file);
      //MessageBox.Show(contents);
      contents = contents.Replace("{checksumx64}", replace64Checksum);
      //MessageBox.Show(contents);
      File.WriteAllText(file, contents);
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
    }
  }
}

// attempt to fix chocopkgup failure when faced with letters in version variable
// ...what was I thinking? no, seriously.
/*
int checkBeta = varVersion.Split('-').Length;
if ( checkBeta == 2) {
  string strPre = varVersion.Split('-')[1];
  string strReplace = String.Concat(strPre, "</version>");
   string strCheck = String.Concat(".", DateTime.Now.ToString("yyyyMMdd"), "</version>");
   string[] fileNuspec = Directory.GetFiles(pkgPath, "*.nuspec", SearchOption.AllDirectories);
  foreach (string file in fileNuspec) {
    string contents = File.ReadAllText(file);
    contents = contents.Replace(strCheck, strReplace);
    File.WriteAllText(file, contents);
  }
}
*/

// delete existing nupkg made by chocopkgup; we leverage chocopkgup to create the structure/files in destination  
System.Diagnostics.Process process1 = new System.Diagnostics.Process();
System.Diagnostics.ProcessStartInfo proc1 = new System.Diagnostics.ProcessStartInfo();
proc1.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
proc1.UseShellExecute = true;
proc1.WorkingDirectory = pkgPath;
proc1.FileName = "cmd.exe";
proc1.Arguments = "/c "+"del /f " + fileUriNupkg;
process1.StartInfo = proc1;
process1.Start();

// create a new nupkg
System.Diagnostics.Process process2 = new System.Diagnostics.Process();
System.Diagnostics.ProcessStartInfo proc2 = new System.Diagnostics.ProcessStartInfo();
proc2.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
proc2.UseShellExecute = true;
proc2.WorkingDirectory = pkgPath;
proc2.FileName = "cmd.exe";
proc2.Arguments = "/c "+"choco pack "+fileUriNuspec+" -d";
process2.StartInfo = proc2;
process2.Start();

// push the nupkg
if (varCScript == "2") {
  System.Diagnostics.Process process3 = new System.Diagnostics.Process();
  System.Diagnostics.ProcessStartInfo proc3 = new System.Diagnostics.ProcessStartInfo();
  proc3.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
  proc3.UseShellExecute = true;
  proc3.WorkingDirectory = pkgPath;
  proc3.FileName = "cmd.exe";
  // find nupkg in pkgPath
  string[] pushPkg = Directory.GetFiles(pkgPath, "*.nupkg", SearchOption.TopDirectoryOnly);
  foreach (String file in pushPkg) {
    proc3.Arguments = "/c "+"cpush " + file + " -d";
  }
  process3.StartInfo = proc3;
  System.Threading.Thread.Sleep(2000);
  process3.Start();
}
}