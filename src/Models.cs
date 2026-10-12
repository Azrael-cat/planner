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
    public class Project  {
        public string Id=Guid.NewGuid().ToString("N");
        public string Title="";
        public int Quantity=1;
        public string Start="";
        public string Deadline="";
        public int ReviewDays=0;
        public bool Archived=false;
    }
    public class Item  {
        public string Id="";
        public string ProjectId="";
        public string Title="";
        public string Kind="任务";
        public int Number;
        public string Due="";
        public string Done="";
        public string Origin="";
    }
    public class Journal  {
        public string Date="";
        public string Text="";
    }
    public class Settings  {
        public int[] Weights=new int[] {
            0,20,20,20,20,20,0
        }
        ;
        public string WorkStart="09:00";
        public string WorkEnd="18:00";
        public string SummaryTime="21:00";
        public bool Topmost=true;
        public int X=-1;
        public int Y=-1;
        public int MiniX=-1;
        public int MiniY=-1;
    }
    public class State  {
        public List<Project> Projects=new List<Project>();
        public List<Item> Items=new List<Item>();
        public List<Journal> Journals=new List<Journal>();
        public Settings Settings=new Settings();
        public string LastPlan="";
        public string LastReminder="";
    }
}
