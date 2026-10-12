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
    public static class GroupTests  {
        static void Check(bool value,string message) {
            if(!value)throw new Exception(message);
        }
        public static void Run() {
            var day=new DateTime(2026,10,12);
            var s=new State();
            var p=new Project {
                Title="同一任务",Quantity=120,Start=Engine.Key(day),Deadline=Engine.Key(day),ReviewDays=7
            }
            ;
            s.Projects.Add(p);
            Engine.Plan(s,day);
            var g=TaskGroups.Today(s,day).Single();
            Check(g.Total==120&&g.Completed==0,"120 units must form one row");
            TaskGroups.SetCompleted(s,g,35,day);
            Check(g.Completed==35&&s.Items.Count(i=>i.Kind=="复习")==35,"batch completion creates reviews");
            TaskGroups.SetCompleted(s,g,35,day);
            Check(s.Items.Count(i=>i.Kind=="复习")==35,"same count is idempotent");
            TaskGroups.SetCompleted(s,g,20,day);
            Check(g.Completed==20&&s.Items.Count(i=>i.Kind=="复习")==20,"decrease removes only undone reviews");
            var review=s.Items.First(i=>i.Kind=="复习");
            review.Done=Engine.Key(day);
            bool rejected=false;
            try {
                TaskGroups.SetCompleted(s,g,0,day);
            }
            catch(InvalidOperationException) {
                rejected=true;
            }
            Check(rejected&&g.Completed==20,"locked undo is atomic");
            TaskGroups.SetCompleted(s,g,1,day);
            Check(g.Completed==1,"reversible units can be reduced");
            Check(TaskGroups.Today(s,day).Count==2,"review and task remain separate");
            var p2=new Project {
                Title=p.Title,Quantity=2,Start=p.Start,Deadline=p.Deadline
            }
            ;
            s.Projects.Add(p2);
            Engine.Plan(s,day);
            Check(TaskGroups.Today(s,day).Count(x=>x.Kind=="任务")==2,"same name projects remain distinct");
            s.Items.Add(new Item {
                Id="a",Title="附加",Kind="附加",Due=Engine.Key(day),Origin="总结"
            }
            );
            s.Items.Add(new Item {
                Id="b",Title="附加",Kind="附加",Due=Engine.Key(day),Origin="总结"
            }
            );
            Check(TaskGroups.Today(s,day).Single(x=>x.Kind=="附加").Total==2,"matching extras group");
            bool range=false;
            try {
                TaskGroups.SetCompleted(s,g,121,day);
            }
            catch(ArgumentOutOfRangeException) {
                range=true;
            }
            Check(range&&g.Completed==1,"invalid quantity leaves state intact");
        }
    }
}
