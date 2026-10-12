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
    public static class Engine  {
        public static string Key(DateTime d)  {
            return d.ToString("yyyy-MM-dd");
        }
        public static DateTime Date(string s)  {
            return DateTime.ParseExact(s,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
        }
        public static bool AdvanceDay(State s,DateTime now) {
            if(s.LastPlan==Key(now))return false;
            Plan(s,now.Date);
            return true;
        }
        public static int MillisecondsUntilMidnight(DateTime now) {
            return Math.Max(1,(int)Math.Ceiling((now.Date.AddDays(1)-now).TotalMilliseconds));
        }
        public static void Plan(State s, DateTime today, bool redistribute=false)  {
            string now=Key(today);
            foreach(Project p in s.Projects.Where(p=>!p.Archived))  {
                for(int n=1;n<=p.Quantity;n++)  {
                    string id=p.Id+":"+n;
                    if(!s.Items.Any(i=>i.Id==id)) s.Items.Add(new Item {
                        Id=id, ProjectId=p.Id, Title=p.Title, Number=n
                    }
                    );
                }
                var pending=s.Items.Where(i=>i.ProjectId==p.Id&&i.Kind=="任务"&&i.Done==""&&(i.Due==""||(redistribute&&String.CompareOrdinal(i.Due,now)>0))).OrderBy(i=>i.Number).ToList();
                if(pending.Count==0)continue;
                DateTime earliest=s.Items.Any(i=>i.ProjectId==p.Id&&i.Kind=="任务"&&i.Due!="")?today.AddDays(1):today;
                DateTime start=Date(p.Start)>earliest?Date(p.Start):earliest, end=Date(p.Deadline);
                var days=new List<DateTime>();
                var weights=new List<int>();
                for(DateTime d=start;d<=end;d=d.AddDays(1))  {
                    int w=s.Settings.Weights[(int)d.DayOfWeek];
                    if(w>0)  {
                        days.Add(d);
                        weights.Add(w);
                    }
                }
                if(days.Count==0)  {
                    foreach(Item i in pending) i.Due=Key(end<today?today:end);
                    continue;
                }
                int count=pending.Count, sum=weights.Sum(), assigned=0;
                double accum=0;
                for(int d=0;d<days.Count;d++)  {
                    accum+=(double)weights[d]*count/sum;
                    int target=d==days.Count-1?count:(int)Math.Round(accum,MidpointRounding.AwayFromZero);
                    while(assigned<target) pending[assigned++].Due=Key(days[d]);
                }
            }
            s.LastPlan=now;
        }
        public static List<Item> Today(State s,DateTime day)  {
            string date=Key(day);
            return s.Items.Where(i=>(i.Done==""&&String.CompareOrdinal(i.Due,date)<=0||i.Done==date)&&!s.Projects.Any(p=>p.Id==i.ProjectId&&p.Archived)).OrderBy(i=>i.Done!="").ThenBy(i=>i.Due).ThenBy(i=>i.Kind).ThenBy(i=>i.ProjectId).ThenBy(i=>i.Number).ToList();
        }
        public static void Complete(State s,Item item,DateTime day)  {
            item.Done=Key(day);
            if(item.Kind!="任务")return;
            Project p=s.Projects.FirstOrDefault(x=>x.Id==item.ProjectId);
            if(p!=null&&p.ReviewDays>0&&!s.Items.Any(i=>i.Id==item.Id+":review"))s.Items.Add(new Item {
                Id=item.Id+":review",ProjectId=p.Id,Title=p.Title,Kind="复习",Number=item.Number,Due=Key(day.AddDays(p.ReviewDays)),Origin="来自 "+Key(day)+" 完成的任务"
            }
            );
        }
        public static bool Undo(State s,Item item)  {
            Item review=s.Items.FirstOrDefault(i=>i.Id==item.Id+":review");
            if(review!=null&&review.Done!="")return false;
            if(review!=null)s.Items.Remove(review);
            item.Done="";
            return true;
        }
    }
}
