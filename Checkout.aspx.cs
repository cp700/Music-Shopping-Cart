﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Data;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.Security;
using System.IO;
using System.Text;
using System.Net.Mail;

namespace ids410ProjSu2020
{
    public partial class Checkout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CC_No"] != null)
            {
                //declare and initialize connection object to connect to the database
                SqlConnection conn = new SqlConnection(
                    WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
                SqlCommand cmd; //declare a command object that will be used to send commands to database.
                SqlParameter orderIDParam; //declare a parameter object that will be passed to a stored procedure
                SqlParameter CC_NoParam; //declare another parameter object
                SqlParameter userIDParam; //declare another parameter object

                conn.Open(); //open a connection to the database
                cmd = conn.CreateCommand(); //create a command object

                orderIDParam = new SqlParameter();
                orderIDParam.ParameterName = "@outOrderID";
                orderIDParam.SqlDbType = SqlDbType.Int;
                orderIDParam.Direction = ParameterDirection.Output;

                CC_NoParam = new SqlParameter();
                CC_NoParam.ParameterName = "@inCC_No";
                CC_NoParam.SqlDbType = SqlDbType.VarChar;
                CC_NoParam.Direction = ParameterDirection.Input;
                CC_NoParam.Value = Session["CC_No"];

                userIDParam = new SqlParameter();
                userIDParam.ParameterName = "@inUserID";
                userIDParam.SqlDbType = SqlDbType.NVarChar;
                userIDParam.Direction = ParameterDirection.Input;
                userIDParam.Value = Session["userID"];

                cmd.Parameters.Add(orderIDParam); //add the first parameter
                cmd.Parameters.Add(CC_NoParam); //add the second parameter
                cmd.Parameters.Add(userIDParam); //add the third parameter
                cmd.CommandText = "pNewOrder";  //this is the name of the stored procedure
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery(); //send the command to execute the stored procedure

                //get the newly generated orderID from the output parameter and store it in
                //a session variable. This way, we can use it later as a foreign key in OrderLine table
                Session["orderID"] = (int)orderIDParam.Value;

                //insert all current user's items in Shopcart into OrderLine table 
                cmd.CommandText = "Insert into Orderline (OrderID, SongID, Quantity) " +
                                  "Select " + Session["orderID"] + ", ShopcartLine.SongID, Quantity " +
                                  "From ShopcartLine inner join Song on ShopcartLine.SongID=Song.SongID " +
                                  "Where shopCartID = " + Session["shopCartID"];
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        protected void Page_LoadComplete(object sender, EventArgs e)
        {
            //send an email to user to confirm purchase
            //MembershipUser usr = Membership.GetUser(Session["userName"].ToString());
            //string strEmailTo = usr.Email.ToString();
            string strEmailTo = User.Identity.Name;
            string strEmailFrom = "tempids410pro@gmail.com"; //put a valid gmail account here to represent the company's email.
            string strEmailSubject = "Purchase Confirmation";
            string strHTML = ""; //prepare a string variable to store rendered html
            string strEmailBody = ""; //prepare a string variable to store email body

            //calculate total purchase
            double total = 0;
            foreach (GridViewRow gvr in GridView1.Rows)
            {
                total = total + double.Parse(gvr.Cells[8].Text.ToString());
            }

            //convert GridView1 to HTML
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            HtmlTextWriter htmlTW = new HtmlTextWriter(sw);
            GridView1.RenderControl(htmlTW);
            strHTML = sb.ToString();

            strEmailBody = "<html><body>";
            strEmailBody =
                   strEmailBody + "You have purchased the following song(s) from us:<br/><br/>";
            strEmailBody = strEmailBody + strHTML;
            strEmailBody = strEmailBody + "<br/><br/>Total Amount: $" + total + ".";
            strEmailBody = strEmailBody + "<br/><br/>Thank you for shopping at our E-store.";
            strEmailBody = strEmailBody + "<br/><br/>- IDS 410 Online Music Store";
            strEmailBody = strEmailBody + "<br/><br/>And We appreciate your business with us.";
            strEmailBody = strEmailBody + "</body></html>";

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(strEmailFrom);
            msg.To.Add(strEmailTo);
            msg.Subject = strEmailSubject;
            msg.IsBodyHtml = true;
            msg.Body = strEmailBody;
            System.Net.Mail.SmtpClient smtpClient =
                new System.Net.Mail.SmtpClient("smtp.gmail.com");
            smtpClient.EnableSsl = true;

            //Change "yourAccount@gmail.com" (see the code segment below) with a valid Gmail account and change "password"
            //   with the password for that valid Gmail account.
            //Note 1: Because you need to reveal the password, you want to create a new, throw-away, Gmail account just
            //	 for this project.
            //Note 2: The first time you use this Checkout.aspx.cs page, you will get an error message. The reason is
            //	because you try to login via an application - Google considers this a less-secure login. To fix this error,
            //  manually, login to your Gmail account. You should have received an email from Google that contains a link
            //  to enable login in a less secured manner. Follow that link and enable a less secured login. You then logout
            //  from your Gmail account. After this, try to submit another order via your website. This time, your website
            //	should be able to send a purchase confirmation email to the user's email address (username). 		
            smtpClient.Credentials =
                new System.Net.NetworkCredential("tempids410pro@gmail.com", "Tempproids@410");
         
            //send email
            smtpClient.Port = 587;
            smtpClient.Send(msg);

            //This transaction is done, clear session variables shopcartID and CC_No
            Session["shopCartID"] = null;
            Session["CC_No"] = null;

            //redirect to download page from which user can download purchased song(s)
            Response.Redirect("download.aspx", true);

        } //end of Page_LoadComplete

        public override void VerifyRenderingInServerForm(Control control)
        {
            // Confirms that an HtmlForm control is rendered for the
            // specified ASP.NET server control at run time.
            // No code required here.
        }
    }
}
