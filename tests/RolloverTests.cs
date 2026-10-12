using System;
using System.Linq;
namespace DeskTasks  {
    public static class RolloverTests  {
        static void Check(bool value,string name) {
            if(!value)throw new Exception("Rollover: "+name);
        }
        public static void Run() {
            DateTime monday=new DateTime(2026,10,12);
            var s=new State();
            s.Projects.Add(new Project {
                Title="每日任务",Quantity=10,Start=Engine.Key(monday),Deadline=Engine.Key(monday.AddDays(4)),ReviewDays=7
            }
            );
            Engine.AdvanceDay(s,monday);
            var first=TaskGroups.Today(s,monday).Single();
            TaskGroups.SetCompleted(s,first,1,monday);
            var original=s.Items.Where(i=>i.Kind=="任务").ToDictionary(i=>i.Id,i=>i.Due);
            Check(!Engine.AdvanceDay(s,monday.AddHours(23).AddMinutes(59).AddSeconds(59)),"before midnight unchanged");
            Check(Engine.AdvanceDay(s,monday.AddDays(1)),"midnight advances");
            var next=TaskGroups.Today(s,monday.AddDays(1)).Single();
            Check(next.Total==3&&next.Completed==0,"one unfinished plus two new");
            Check(next.Carried(monday.AddDays(1))==1&&next.NewToday(monday.AddDays(1))==2,"carry breakdown");
            Check(s.Items.Where(i=>i.Kind=="任务").All(i=>i.Due==original[i.Id]),"original due dates preserved");
            Check(!Engine.AdvanceDay(s,monday.AddDays(1).AddSeconds(1)),"second tick idempotent");
            Engine.AdvanceDay(s,monday.AddDays(3));
            Check(TaskGroups.Today(s,monday.AddDays(3)).Single().Total==7,"missed days accumulate");
            Engine.AdvanceDay(s,monday.AddDays(5));
            Check(TaskGroups.Today(s,monday.AddDays(5)).Single().Total==9,"rest day retains backlog");
            Check(s.Items.Count(i=>i.Kind=="任务")==10,"no duplicates");
            s.Items.Add(new Item {
                Id="extra",Kind="附加",Title="附加",Due=Engine.Key(monday)
            }
            );
            Engine.AdvanceDay(s,monday.AddDays(8));
            Check(Engine.Today(s,monday.AddDays(8)).Any(i=>i.Kind=="复习"),"overdue review carries");
            Check(Engine.Today(s,monday.AddDays(8)).Any(i=>i.Id=="extra"),"extra carries");
            Check(Engine.MillisecondsUntilMidnight(monday.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(900))==100,"100ms until midnight");
            Check(Engine.MillisecondsUntilMidnight(monday)==86400000,"schedule next midnight");
            var manual=new State();
            manual.Projects.Add(new Project {
                Title="设置变更",Quantity=10,Start=Engine.Key(monday),Deadline=Engine.Key(monday.AddDays(4))
            }
            );
            Engine.Plan(manual,monday);
            Engine.Plan(manual,monday.AddDays(1),true);
            Check(manual.Items.Count(i=>i.Due==Engine.Key(monday))==2,"manual edit retains overdue");
            Check(manual.Items.Count(i=>i.Due==Engine.Key(monday.AddDays(1)))==2,"manual edit retains today");
            TaskGroups.SetCompleted(s,TaskGroups.Today(s,monday.AddDays(8)).First(g=>g.Kind=="任务"),9,monday.AddDays(8));
            Check(s.Items.Count(i=>i.Kind=="任务"&&i.Done!="")==10,"backlog completion counted once");
            Check(Engine.Today(s,monday.AddDays(9)).All(i=>i.Kind!="任务"),"completed backlog absent next day");
        }
    }
}
