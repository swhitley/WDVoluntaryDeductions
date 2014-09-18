WD Voluntary Deductions
=======================

[MIT License](http://en.wikipedia.org/wiki/MIT_License)

Visual Studio 2012, C#, MVC

This is a sample web app for enabling self-service of pay input for voluntary deductions in **Workday**.  
One-time deductions are allowed, however, this app does not create ongoing deductions.

This is a sample only.  Feel free to modify it for your needs and test it thoroughly in your environment.

Instructions
------------

**1)** Create a Workday report and enable it as a web service.  See **Integration_Voluntary_Deductions.pdf**, included in the solution.  The report provides data that is used to display current deductions and allows the app to determine the correct dates for the pay input.
The report is filtered by specific deductions.  The deductions in the filter should match the dropdown in the web page (configured below).

**2)** Update the **web.config** file for your Workday environment.


**<AppSettings>**

**WD_CRED** = A Workday username and the Workday password for the user, separated by a comma.  This user must have access to the Workday report from Step 1.

**WD_VOL_DEDUCTIONS_URL** = The json Workday web service endpoint for the report created in Step 1.  It includes replacement variables (%%variable%%) that the app will use.

Example:

    <!-- WD Settings -->
    <add key="WD_CRED" value="ISU_User,ou8onetw0!" />
    <add key="WD_TENANT" value="gms" />
    <add key="WD_VERSION" value="v22" />
    <add key="WD_VOL_DEDUCTIONS_URL" value="https://wd5-impl-services1.workday.com/ccx/service/customreport2/%%tenant%%/lmcneil/Integration__Voluntary_Deductions?Worker!Employee_ID=%%employeeId%%&amp;format=json" />
    <!-- WD Settings -->      

	Note: You will need to add the replacement %%variables%% to the url for your report.

**Update the public web service endpoint for your environment**

	Example:

     <endpoint address="https://wd5-impl-services1.workday.com/ccx/service/gms/Payroll/v20" binding="customBinding" bindingConfiguration="Payroll_ResourcesBinding" contract="WD_Payroll.PayrollPort" name="Payroll" />


**3)** Configure the Deductions Dropdown in your index.cshtml page.

1. Open */views/voluntarydeductions/index.cshtml*
2. Add to the list of deductions in the dropdown by updating the code.

Example:

    //Update the list of deductions for the dropdown below.
    //  {"Deduction Code", "Deduction Description"}
    ViewBag.DeductionCodes = new Dictionary<string, string>()
    {
         {"DED1", "Deduction 1"}
		,{"DED2", "Deduction 2"}
		,{"DED3", "Deduction 3"}
    };    

**4)** Modify EmployeeIDLookup()

1. Open */controllers/voluntarydeductionscontroller.cs*
2. Modify the EmployeeIDLookup() method for your environment.  The existing method provides a sample translation from the network username to the employee id.  It assumes a local database contains the translation.


Ongoing Pay Input vs. One-time Pay Input
----------------------------------------
The project is setup for one-time input, however, a few minor coding changes will allow the creation of ongoing pay input.

```
//Code changes for ongoing pay input.
//Commented out the end date and changed ongoing_input to true.
//Added Ongoing_InputSpecified
input.Start_Date = effDt;
//input.End_Date = effDt; 
//input.End_DateSpecified = true; 
input.Ongoing_Input = true;
input.Ongoing_InputSpecified = true;
```


![Screenshot](/Capture.PNG?raw=true "")

