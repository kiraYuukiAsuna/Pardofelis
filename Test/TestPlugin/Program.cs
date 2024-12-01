using CdnDownload;
using SingPlugin;
using ImageRecognitionPlugin;

var downloadPath = Path.Join("Download", "Resources");
Dl123Pan.DownloadByPathAndName("/directlink/Resources", "BuitlinApiKey.json",
    downloadPath, "BuitlinApiKey.json");