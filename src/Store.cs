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
    public static class Store  {
        public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DeskTaskAssistant");
        public static string FilePath  {
            get {
                return Path.Combine(Root,"data.json");
            }
        }
        public static JavaScriptSerializer Json=new JavaScriptSerializer {
            MaxJsonLength=50000000
        }
        ;
        public static State Load() {
            Directory.CreateDirectory(Root);
            if(!File.Exists(FilePath))return new State();
            try {
                return Read(FilePath);
            }
            catch(Exception) {
                if(File.Exists(FilePath+".bak")) {
                    State backup=Read(FilePath+".bak");
                    MessageBox.Show("主数据文件损坏，已读取上次备份。","数据恢复");
                    return backup;
                }
                throw new Exception("数据无法读取。请先保留数据文件并从备份恢复："+FilePath);
            }
        }
        public static State Read(string path) {
            State s=Json.Deserialize<State>(File.ReadAllText(path,Encoding.UTF8));
            if(s==null||s.Projects==null||s.Items==null||s.Journals==null||s.Settings==null||s.Settings.Weights==null||s.Settings.Weights.Length!=7||s.Settings.Weights.Any(w=>w<0||w>100)||s.Settings.Weights.Sum()==0)throw new Exception("备份格式无效");
            foreach(Project p in s.Projects) {
                Engine.Date(p.Start);
                Engine.Date(p.Deadline);
                if(p.Quantity<1||p.Quantity>10000||p.ReviewDays<0||p.ReviewDays>3650)throw new Exception("任务数据无效");
            }
            foreach(Item i in s.Items) {
                Engine.Date(i.Due);
                if(i.Done!="")Engine.Date(i.Done);
            }
            return s;
        }
        [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)] static extern bool MoveFileEx(string source,string destination,int flags);
        public static void Save(State s) {
            Directory.CreateDirectory(Root);
            string tmp=FilePath+".tmp";
            File.WriteAllText(tmp,Json.Serialize(s),Encoding.UTF8);
            if(File.Exists(FilePath))File.Copy(FilePath,FilePath+".bak",true);
            if(!MoveFileEx(tmp,FilePath,11))throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        }
    }
}
