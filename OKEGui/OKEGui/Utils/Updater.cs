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
        private static readonly string Title = "OKEGui Updater";

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


        public static async void CheckUpdate(bool interactive=false)
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
                    Show($"[{httpResponseMessage.StatusCode}]请求发送失败，错误信息:\n{body}", Logger.Error);
                    return;
                }

                GithubRelease result;
                using (var streamReader = new StreamReader(await httpResponseMessage.Content.ReadAsStreamAsync()))
                {
                    result = JsonConvert.DeserializeObject<GithubRelease>(streamReader.ReadToEnd());
                }

                var remoteVersion = Version.Parse(Convert.ToString(result.tag_name));
                Logger.Info($"本地版本: v{CurrentVersion}, 远端版本: v{remoteVersion}");
                if (remoteVersion <= CurrentVersion)
                {
                    Show($"{CurrentVersion}已为最新版", Logger.Info);
                    return;
                }

                var validAsset = result.assets.FirstOrDefault(asset =>
                    asset.content_type == "application/x-msdownload" ||
                    asset.content_type == "application/x-zip-compressed");
                if (validAsset != null)
                {
                    Logger.Info($"发现新版本v{remoteVersion}, 下载地址: {validAsset.browser_download_url}");
                    var dialogResult = MessageBox.Show(caption: Title,
                        text: $"发现新版本v{remoteVersion}，是否现在下载",
                        buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Asterisk);
                    if (dialogResult != DialogResult.Yes) return;
                    var proc = new Process
                    {
                        StartInfo = { UseShellExecute = true, FileName = validAsset.browser_download_url }
                    };
                    proc.Start();
                }
                else
                {
                    Show($"发现新版本v{remoteVersion}，但无可用的资源", Logger.Error);
                }
            }
            catch (TaskCanceledException)
            {
                Show("请求超时", Logger.Error);
            }
            catch (Exception e)
            {
                Show($"[{e.GetType()}]请求失败: {e.Message}，${e.InnerException}", Logger.Error);
            }

            void Show(string message, Action<string> logger)
            {
                if (interactive)
                {
                    MessageBox.Show(message, Title);
                }
                else
                {
                    logger(message);
                }
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
