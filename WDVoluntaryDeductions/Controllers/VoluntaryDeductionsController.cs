#region License
/*

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WDVoluntaryDeductions.Models;
using WDVoluntaryDeductions.WD_Payroll;
using System.Configuration;
using System.ServiceModel;
using System.Data.SqlClient;
using System.Net;
using Newtonsoft.Json;

namespace WDVoluntaryDeductions.Controllers
{
    public class VoluntaryDeductionsController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                //Lookup by Username
                string employeeId = EmployeeIDLookup();

                //Retrieve the current payroll input.
                ViewBag.Payroll = CurrentInputGet(employeeId);

                //Message Returned From the Post
                if (Request["message"] != null)
                {
                    ViewBag.Message = Request["message"].ToString();
                }

                //Message Style
                ViewBag.MessageStyle = "error";
                if (Request["message_style"] != null)
                {
                    ViewBag.MessageStyle = Request["message_style"].ToString();
                }
            }
            catch (Exception ex)
            {
                ViewBag.MessageStyle = "error";
                ViewBag.Message = ex.Message;
            }
            
            return View(new Payroll_Input());
        }

        [HttpPost]
        public ActionResult Index(Payroll_Input newPayInput)
        {
            string employeeId = "";
            string wid = "";

            try
            {
                //Lookup by Username
                employeeId = EmployeeIDLookup();

                //Validate the Amount
                decimal amount = 0;
                if (!decimal.TryParse(newPayInput.AMOUNT, out amount) || amount < 0)
                {
                    throw new Exception("Error: You have entered an invalid amount.");
                }

                //Check for an existing pay input for this deduction code.
                //Override the amount if the deduction already exists.
                Payroll payroll = CurrentInputGet(employeeId);
                
                if (payroll.Payroll_Inputs != null)
                {
                    foreach (Payroll_Input payInput in payroll.Payroll_Inputs)
                    {
                        //Retrieve the Workday ID for the matching deduction code.
                        if (newPayInput.DEDCD == payInput.DEDCD)
                        {
                            wid = payInput.WID;
                        }
                    }
                }

                //Workday Payroll Client
                PayrollPortClient client = new PayrollPortClient("Payroll");
                string tenant = ConfigurationManager.AppSettings["WD_TENANT"];
                string version = ConfigurationManager.AppSettings["WD_VERSION"];
                string dedCode = newPayInput.DEDCD;


                //Credentials
                string[] cred = null;
                cred = ConfigurationManager.AppSettings["WD_CRED"].Split(',');
                client.ClientCredentials.UserName.UserName = cred[0] + "@" + tenant;
                client.ClientCredentials.UserName.Password = cred[1];


                //** Get an existing pay input **
                Get_Submit_Payroll_Inputs_ResponseType getResp = null;
                if (wid.Length > 0)
                {
                    //Request Type
                    Get_Submit_Payroll_Inputs_RequestType getRqst = new Get_Submit_Payroll_Inputs_RequestType();

                    getRqst.version = version;

                    //References Type
                    Payroll_Input_Request_ReferencesType getRefs = new Payroll_Input_Request_ReferencesType();
                    getRefs.Payroll_Input_Reference = new Payroll_InputObjectType[1];

                    //Input Type
                    Payroll_InputObjectIDType inputID = new Payroll_InputObjectIDType();
                    inputID.type = "WID";
                    inputID.Value = wid;
                    Payroll_InputObjectType inputObj = new Payroll_InputObjectType();
                    inputObj.ID = new Payroll_InputObjectIDType[1];
                    inputObj.ID.SetValue(inputID, 0);

                    //Set References
                    getRefs.Payroll_Input_Reference.SetValue(inputObj, 0);
                    getRqst.Item = getRefs;

                    //Get Pay Input Object
                    getResp = client.Get_Submit_Payroll_Inputs(getRqst);
                }


                //Submit Payroll Input Request
                Submit_Payroll_Input_RequestType rqst = new Submit_Payroll_Input_RequestType();
                rqst.version = version;


                if (wid.Length > 0)
                {
                    //Update the existing deduction.
                    if (getResp != null && getResp.Response_Data != null)
                    {
                        getResp.Response_Data[0].Payroll_Input_Data[0].Amount = decimal.Parse(newPayInput.AMOUNT);
                        rqst.Payroll_Input_Data = getResp.Response_Data[0].Payroll_Input_Data;
                    }
                    else
                    {
                        throw new Exception("Error: You already have an existing deduction for this deduction type and the current deduction cannot be updated.");
                    }
                }
                else
                {
                    //Input Data
                    Submit_Payroll_Input_DataType input = new Submit_Payroll_Input_DataType();

                    input.Amount = decimal.Parse(newPayInput.AMOUNT);
                    input.AmountSpecified = true;

                    //Effective date for the pay input.
                    DateTime effDt = DateTime.Parse(payroll.PAY_END_DT).AddDays(1);

                    input.Batch_ID = "VOLUNTARY:" + effDt.ToString("yyyy-MM-dd");
                    //This is a one-time pay input starting and ending on the first day after the last completed payroll.
                    input.Start_Date = effDt;
                    input.End_Date = effDt;
                    input.End_DateSpecified = true;
                    input.Ongoing_Input = false;

                    //Deduction Object
                    Deduction__All_ObjectIDType dedID = new Deduction__All_ObjectIDType();
                    dedID.type = "Deduction_Code";
                    dedID.Value = dedCode;
                    Deduction__All_ObjectType dedObj = new Deduction__All_ObjectType();
                    dedObj.ID = new Deduction__All_ObjectIDType[1];
                    dedObj.ID.SetValue(dedID, 0);
                    input.Item = dedObj;

                    //Worker Object
                    WorkerObjectIDType wkID = new WorkerObjectIDType();
                    wkID.type = "Employee_ID";
                    wkID.Value = employeeId;
                    WorkerObjectType wk = new WorkerObjectType();
                    wk.ID = new WorkerObjectIDType[1];
                    wk.ID.SetValue(wkID, 0);
                    input.Worker_Reference = wk;

                    //Assign the input data to the request.
                    rqst.Payroll_Input_Data = new Submit_Payroll_Input_DataType[1];
                    rqst.Payroll_Input_Data.SetValue(input, 0);
                }


                //Update Workday
                client.Submit_Payroll_Input(rqst);

                //Was this a new entry or an update?
                string addedOrUpdated = "Added";
                if (wid.Length > 0)
                {
                    addedOrUpdated = "Updated";
                }


                //Notify Payroll
                //Common.EmailSend(ConfigurationManager.AppSettings["VolDedSMTPTo"].ToString(), "Voluntary Deduction " + addedOrUpdated,
                //    "Employee Id: " + employeeId
                //    + "<br/>User: " + User.Identity.Name
                //    + "<br/>Deduction: " + newPayInput.DEDCD
                //    + "<br/>Amount: " + newPayInput.AMOUNT
                //    );

                ViewBag.Message = "Your deduction was successfully " + addedOrUpdated.ToLower() + ".";
                ViewBag.MessageStyle = "message-success";

                //Redirect to self.
                string page = Request.Url.AbsoluteUri;
                if (page.IndexOf("?") > 0)
                {
                    page = page.Substring(0, page.IndexOf("?"));
                }
                
                Response.Redirect(page + "?message=" + HttpUtility.UrlEncode(ViewBag.Message) + "&message_style=" + HttpUtility.UrlEncode(ViewBag.MessageStyle));

            }
            catch (Exception ex)
            {
                ViewBag.MessageStyle = "error";
                ViewBag.Message = ex.Message;
            }


            //On error we will update the list again.
            try
            {
                //Retrieve the updated list of pay input.
                ViewBag.Payroll = CurrentInputGet(employeeId);
            }
            catch (Exception ex)
            {
                ViewBag.MessageStyle = "error";
                ViewBag.Message = ex.Message;
            }
            

            return View(newPayInput);
        }

        private Payroll CurrentInputGet(string employeeId)
        {
            //Workday Credentials
            string[] cred = null;
            cred = ConfigurationManager.AppSettings["WD_CRED"].Split(',');

            //Download the data via RaaS to display any existing payroll input.
            WebClient web = new WebClient();
            web.Credentials = new NetworkCredential(cred[0], cred[1]);
            string url = ConfigurationManager.AppSettings["WD_VOL_DEDUCTIONS_URL"];
            url = url.Replace("%%employeeId%%", employeeId).Replace("%%tenant%%", ConfigurationManager.AppSettings["WD_TENANT"]);

            return JsonConvert.DeserializeObject<VoluntaryDeduction>(web.DownloadString(url)).Report_Entry[0];
        }


        private string EmployeeIDLookup()
        {
            string username = User.Identity.Name.Substring(User.Identity.Name.LastIndexOf("\\") + 1).ToLower();

            //Database lookup for employee id by username.
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["default"].ToString()))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT EMPLID FROM PS_PERSONNEL WHERE NTLOGIN=@LOGIN AND PERSNL_STATUS='E' AND EMP_STATUS IN ('A','L','P','S','Q','R')", conn);
                cmd.Parameters.Add(new SqlParameter("@LOGIN", username));
                object id = cmd.ExecuteScalar();
                if (id != null)
                {
                    return id.ToString();
                }
                else
                {
                    throw new Exception("Error: A lookup of your employee id failed.  Please contact your payroll administrator.");
                }
            }
        }
    }
}
