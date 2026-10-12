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
    static class Tests  {
        public static string ResultPath {
            get {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "verification");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "test-results.txt");
            }
        }
        static void Check(bool ok,string name) {
            if(!ok)throw new Exception("FAILED: "+name);
        }
        public static void Run() {
            DateTime monday=new DateTime(2026,10,12);
            var s=new State();
            var p=new Project {
                Title="测试",Quantity=10,Start=Engine.Key(monday),Deadline=Engine.Key(monday.AddDays(4)),ReviewDays=7
            }
            ;
            s.Projects.Add(p);
            Engine.Plan(s,monday);
            Check(s.Items.Count==10,"quantity");
            Check(Engine.Today(s,monday).Count==2,"weekday allocation");
            Item first=Engine.Today(s,monday)[0];
            Engine.Complete(s,first,monday);
            Check(s.Items.Count(i=>i.Kind=="复习"&&i.Due=="2026-10-19")==1,"review date");
            Engine.Complete(s,first,monday);
            Check(s.Items.Count(i=>i.Kind=="复习")==1,"idempotent review");
            Check(Engine.Undo(s,first),"undo");
            Check(s.Items.Count==10,"undo removes review");
            Engine.Complete(s,first,monday);
            Engine.Plan(s,monday.AddDays(1));
            Check(s.Items.Count(i=>i.Kind=="任务")==10,"no duplicates");
            Check(Engine.Today(s,monday.AddDays(1)).Count(i=>i.Kind=="任务")==3,"carry incomplete into next day");
            Check(first.Done=="2026-10-12","preserve completed");
            Engine.Plan(s,monday.AddDays(8));
            Check(Engine.Today(s,monday.AddDays(8)).Count(i=>i.Kind=="任务")==9,"all overdue tasks visible");
            Check(Engine.Today(s,monday.AddDays(8)).Any(i=>i.Kind=="复习"),"overdue review visible");
            s.Items.Add(new Item {
                Id="extra",Title="附加",Kind="附加",Due="2026-10-23"
            }
            );
            Check(!Engine.Today(s,monday.AddDays(8)).Any(i=>i.Id=="extra"),"future hidden");
            Check(Engine.Today(s,monday.AddDays(11)).Any(i=>i.Id=="extra"),"future appears");
            p.Archived=true;
            Check(!Engine.Today(s,monday.AddDays(8)).Any(i=>i.ProjectId==p.Id),"archived hidden");
            var weighted=new State();
            weighted.Settings.Weights=new int[] {
                0,80,20,0,0,0,0
            }
            ;
            weighted.Projects.Add(new Project {
                Title="权重",Quantity=10,Start="2026-10-12",Deadline="2026-10-13"
            }
            );
            Engine.Plan(weighted,monday);
            Check(weighted.Items.Count(i=>i.Due=="2026-10-12")==8,"weighted 80 percent");
            Check(weighted.Items.Count(i=>i.Due=="2026-10-13")==2,"weighted 20 percent");
            string json=Store.Json.Serialize(s);
            Check(Store.Json.Deserialize<State>(json).Items.Count==s.Items.Count,"json round trip");
            File.WriteAllText(Tests.ResultPath,"PASS: 16 scheduling, review, rollover, archive, extra task, and persistence checks.");
        }
    }
}
