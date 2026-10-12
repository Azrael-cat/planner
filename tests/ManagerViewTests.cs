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
    public static class ManagerViewTests  {
        static void Check(bool value,string message) {
            if(!value)throw new Exception("Manager views: "+message);
        }
        public static void Run() {
            var day=new DateTime(2026,10,12);
            var state=new State();
            var a=new Project {
                Title="同名任务",Quantity=120,Start=Engine.Key(day),Deadline=Engine.Key(day),ReviewDays=7
            }
            ;
            var b=new Project {
                Title=a.Title,Quantity=80,Start=a.Start,Deadline=a.Deadline
            }
            ;
            state.Projects.Add(a);
            state.Projects.Add(b);
            Engine.Plan(state,day);
            TaskGroups.SetCompleted(state,TaskGroups.Today(state,day).First(g=>g.Items[0].ProjectId==a.Id),35,day);
            TaskGroups.SetCompleted(state,TaskGroups.Today(state,day).First(g=>g.Items[0].ProjectId==b.Id),4,day);
            var groups=ScheduleGroup.All(state);
            Check(groups.Count==2,"one row per project including its reviews");
            Check(groups.First(g=>g.Project.Id==a.Id).Items.Count==155,"review units remain available in detail");
            var first=ProjectReport.Build(state,a,day);
            var second=ProjectReport.Build(state,b,day);
            Check(first.Done==35&&second.Done==4,"same names keep independent progress");
            Check(first.TodayDone==35&&first.TodayTotal==120,"per project today counts");
            Check(first.Daily.Sum()==35&&second.Daily.Sum()==4,"per project trend counts");
            Check(first.ReviewTotal==35&&first.ReviewDone==0,"review tracked separately");
            Check(Math.Abs(first.Speed-35.0/9)<0.0001,"per project planned hour rate");
            b.Archived=true;
            Check(ScheduleGroup.All(state).Count==2,"archived project remains visible");
            state.Items.Add(new Item {
                Id="extra1",Title="同名任务",Kind="附加",Due=a.Start,Origin="总结"
            }
            );
            state.Items.Add(new Item {
                Id="extra2",Title="同名任务",Kind="附加",Due=a.Start,Origin="总结"
            }
            );
            Check(ScheduleGroup.All(state).Count==3&&ScheduleGroup.All(state).Last().Items.Count==2,"extras do not merge into same named project");
            Check(ProjectReport.Build(state,a,day).Done==35,"extras do not inflate project progress");
        }
    }
}
