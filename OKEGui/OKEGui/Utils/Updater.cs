using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
// ReSharper disable UnusedMember.Global

namespace OKEGui.Utils
{
    static class Updater
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        private static string _softwareName;
        public static string SoftwareName
        {
            set => _softwareName = value;
            get
            {
                if (_softwareName == null)
                {
                    throw new NullReferenceException($"You must set {nameof(SoftwareName)} before using it");
                }
                return _softwareName;
            }
        }

        private static string _repoName;
        public static string RepoName
        {
            set => _repoName = value;
            get
            {
                if (_repoName == null)
                {
                    throw new NullReferenceException($"You must set {nameof(RepoName)} before using it");
                }
                return _repoName;
            }
        }

        private static Version _currentVersion;
        public static Version CurrentVersion
        {
            set => _currentVersion = value;
            get
            {
                if (_currentVersion == null)
                {
                    throw new NullReferenceException($"You must set {nameof(CurrentVersion)} before using it");
                }
                return _currentVersion;
            }
        }


        public static async void CheckUpdate()
        {
            if (!IsConnectInternet()) return;
            try
            {
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(SoftwareName, CurrentVersion.ToString()));
                var httpResponseMessage =
                    await httpClient.GetAsync(new Uri($"https://api.github.com/repos/{RepoName}/releases/latest"));
                if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                {
                    var body = await httpResponseMessage.Content.ReadAsStringAsync();
                    Logger.Error($"[{httpResponseMessage.StatusCode}]请求发送失败，错误信息:\n{body}");
                    return;
                }

                GithubRelease result;
                using (var streamReader = new StreamReader(await httpResponseMessage.Content.ReadAsStreamAsync()))
                {
                    result = JsonConvert.DeserializeObject<GithubRelease>(streamReader.ReadToEnd());
                }

                var remoteVersion = Version.Parse(Convert.ToString(result.tag_name));
                if (remoteVersion <= CurrentVersion)
                {
                    Logger.Info($"{CurrentVersion}已为最新版");
                    return;
                }

                var validAsset = result.assets.FirstOrDefault(asset =>
                    asset.content_type == "application/x-msdownload" ||
                    asset.content_type == "application/x-zip-compressed");
                if (validAsset != null)
                {
                    Logger.Info($"发现新版本v{remoteVersion}, 下载地址: ${validAsset.browser_download_url}");
                    var dialogResult = MessageBox.Show(caption: "OKEGui Updater",
                        text: $"发现新版本v{remoteVersion}，是否现在下载",
                        buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Asterisk);
                    if (dialogResult != DialogResult.Yes) return;
                    var proc = new Process
                    {
                        StartInfo = { UseShellExecute = true, FileName = validAsset.browser_download_url }
                    };
                    proc.Start();
                    return;
                }

                Logger.Error("无可用的资源，请向项目维护人咨询具体情况");
            }
            catch (TaskCanceledException e)
            {
                Logger.Error(e, "请求超时");
            }
            catch (Exception e)
            {
                Logger.Error(e, $"[{e.GetType()}]请求失败: {e.Message}，${e.InnerException}");
            }
        }

        private static bool IsConnectInternet()
        {
            return InternetGetConnectedState(0, 0);
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(int description, int reservedValue);

        public class GithubAsset
        {
            public string url;
            public string name;
            public string content_type;
            public long size;
            public string created_at;
            public string updated_at;
            public string browser_download_url;
        }

        public class GithubRelease
        {
            public string url;
            public string assets_url;
            public string tag_name;
            public string name;
            public List<GithubAsset> assets;
            public string zipball_url;
            public string body;
        }
    }
}
