using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace WDVoluntaryDeductions.Models
{
    public class VoluntaryDeduction
    {
        public List<Payroll> Report_Entry {get; set;}
    }

    public class Payroll
    {
        public string PAY_GROUP {get; set; }
        public string PAY_END_DT {get; set; }
        public List<Payroll_Input> Payroll_Inputs {get; set;}
    }

    public class Payroll_Input
    {
        [Display(Name="Deduction")]
        public string DEDUCTION {get; set;}
        public string DEDCD {get; set;}
        [Display(Name="Amount")]
        public string AMOUNT {get; set;}
        public string END_DATE {get; set;}
        public string WID {get; set;}
    }
}


