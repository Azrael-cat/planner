using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.Threading;
namespace DeskTasks  {
    static class Program  {
        public static string StartupPath  {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"DeskTaskAssistant.lnk");
            }
        }
        public static void SetStartup(bool enabled) {
            if(!enabled) {
                if(File.Exists(StartupPath))File.Delete(StartupPath);
                return;
            }
            Type type=Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell=Activator.CreateInstance(type);
            dynamic link=shell.CreateShortcut(StartupPath);
            link.TargetPath=Application.ExecutablePath;
            link.WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory;
            link.Description="桌面任务小助手";
            link.Save();
        }
        [STAThread] static void Main(string[] args) {
            if(args.Contains("--self-test")) {
                try {
                    Tests.Run();
                    GroupTests.Run();
                    RolloverTests.Run();
                    ManagerViewTests.Run();
                    File.AppendAllText(Tests.ResultPath, "\r\nPASS: grouping, batch quantity, midnight, carryover and per-project reporting checks.");
                    Environment.ExitCode=0;
                }
                catch(Exception ex) {
                    File.WriteAllText(Tests.ResultPath,ex.ToString());
                    Environment.ExitCode=1;
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using(var mutex=new Mutex(false,"Local\\DeskTaskAssistant")) {
                bool acquired=false;
                try {
                    try {
                        acquired=mutex.WaitOne(0);
                    }
                    catch(AbandonedMutexException) {
                        acquired=true;
                    }
                    if(!acquired) {
                        try {
                            using(var signal=EventWaitHandle.OpenExisting("Local\\DeskTaskAssistantReveal"))signal.Set();
                        }
                        catch(WaitHandleCannotBeOpenedException) {
                            MessageBox.Show("小助手正在启动，请稍后再次打开。");
                        }
                        return;
                    }
                    Application.ThreadException+=(s,e)=>MessageBox.Show("操作未完成："+e.Exception.Message+"\n请检查磁盘空间，并导出备份。","桌面任务小助手");
                    Application.Run(new Card(Store.Load()));
                }
                catch(Exception ex) {
                    MessageBox.Show("无法启动："+ex.Message,"桌面任务小助手");
                }
                finally {
                    if(acquired)mutex.ReleaseMutex();
                }
            }
        }
    }
}
