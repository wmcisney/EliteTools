﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RadialReview.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class EmailStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal EmailStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RadialReview.Properties.EmailStrings", typeof(EmailStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;table style=&quot;background-color:#f3f4f4&quot; bgcolor=&quot;#F3F4F4&quot; width=&quot;100%&quot; align=&quot;center&quot; cellspacing=&quot;0&quot; cellpadding=&quot;0&quot;&gt;&lt;tbody&gt;&lt;tr&gt;&lt;td&gt;
        ///            &lt;br&gt;
        ///            &lt;table cellpadding=&quot;1&quot; cellspacing=&quot;0&quot; style=&quot;background-color:#d9dadb&quot; align=&quot;center&quot; bgcolor=&quot;#D9DADB&quot;&gt;&lt;tbody&gt;&lt;tr&gt;&lt;td&gt;
        ///            &lt;table style=&quot;font-family:arial,helvetica,sans-serif;font-size:14px;line-height:20px;background-color:#ffffff&quot; align=&quot;center&quot; bgcolor=&quot;#ffffff&quot; cellspacing=&quot;0&quot; cellpadding=&quot;0&quot; width=&quot;{2}&quot;&gt;
        ///
        ///            &lt;tbody&gt;&lt;t [rest of string was truncated]&quot;;.
        /// </summary>
        public static string BodyWrapper {
            get {
                return ResourceManager.GetString("BodyWrapper", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have a new survey to complete. Please complete the survey here:.
        /// </summary>
        public static string DefaultSurvey_Body {
            get {
                return ResourceManager.GetString("DefaultSurvey_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have a new survey to complete.
        /// </summary>
        public static string DefaultSurvey_Subject {
            get {
                return ResourceManager.GetString("DefaultSurvey_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        public static string FirstCharge_Body {
            get {
                return ResourceManager.GetString("FirstCharge_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This message was generated automatically by EliteTools.&lt;br/&gt; 95 Highland Ave., Bethlehem, PA 18017&lt;br/&gt;If you feel you have received this message in error you can respond to this email..
        /// </summary>
        public static string Footer {
            get {
                return ResourceManager.GetString("Footer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;center&gt;&lt;a href=&quot;#&quot; title=&quot;{4}&quot; target=&quot;_blank&quot;&gt;&lt;img src=&quot;https://elitetools.s3.us-east-2.amazonaws.com/placeholder/Elite_Tools_Logo_Large_Vertical_Wht_A2.png&quot; width=&quot;300&quot; height=&quot;auto&quot; alt=&quot;{4}&quot; /&gt;&lt;/a&gt;&lt;/center&gt;
        ///&lt;br/&gt;
        ///&lt;p&gt;{0},&lt;br/&gt; Join {1} on {4} by clicking the following link and creating a password:&lt;/p&gt;
        ///    &lt;a href=&quot;{2}&quot;&gt;{3}&lt;/a&gt;
        ///&lt;br /&gt;
        ///&lt;br /&gt;If you have any questions about {4}, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {4} Team.
        /// </summary>
        public static string JoinOrganizationUnderManager_Body {
            get {
                return ResourceManager.GetString("JoinOrganizationUnderManager_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}, join {1} on {2}.
        /// </summary>
        public static string JoinOrganizationUnderManager_Subject {
            get {
                return ResourceManager.GetString("JoinOrganizationUnderManager_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;Here is your meeting summary for this week:&lt;/p&gt;
        ///{1}
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {2} Team.
        /// </summary>
        public static string MeetingSummary_Body {
            get {
                return ResourceManager.GetString("MeetingSummary_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;Here is your meeting summary for this week:&lt;/p&gt;
        ///{1}
        ///&lt;br /&gt;.
        /// </summary>
        public static string MeetingSummary_Body_New {
            get {
                return ResourceManager.GetString("MeetingSummary_Body_New", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Meeting Summary - {0}.
        /// </summary>
        public static string MeetingSummary_Subject {
            get {
                return ResourceManager.GetString("MeetingSummary_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;{6} is open until {2}. You can take your eval by clicking the following link:&lt;/p&gt;
        ///&lt;br&gt; 
        ///    &lt;a href=&quot;{3}&quot;&gt;{4}&lt;/a&gt;
        ///&lt;br /&gt;&lt;br /&gt;If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {5} Team.
        /// </summary>
        public static string NewReview_Body {
            get {
                return ResourceManager.GetString("NewReview_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have a new eval from {0}.
        /// </summary>
        public static string NewReview_Subject {
            get {
                return ResourceManager.GetString("NewReview_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;To reset your password please click the following link:&lt;/p&gt;
        ///&lt;br&gt; 
        ///    &lt;a href=&quot;{1}&quot;&gt;{2}&lt;/a&gt;
        ///&lt;br /&gt;&lt;br /&gt;If it was not at your request, then please contact us immediately. If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {3} Team.
        /// </summary>
        public static string PasswordReset_Body {
            get {
                return ResourceManager.GetString("PasswordReset_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} Password Reset.
        /// </summary>
        public static string PasswordReset_Subject {
            get {
                return ResourceManager.GetString("PasswordReset_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;Payment Failed: {0}&lt;/p&gt;&lt;p&gt;{1}&lt;/p&gt;&lt;p&gt;{2}&lt;/p&gt;&lt;p&gt;{3}&lt;/p&gt;.
        /// </summary>
        public static string PaymentException_Body {
            get {
                return ResourceManager.GetString("PaymentException_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Payment Failed for {0}.
        /// </summary>
        public static string PaymentException_Subject {
            get {
                return ResourceManager.GetString("PaymentException_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;This is an invoice for your {0} account. To avoid interruptions in service, please ensure your credit card is up-to-date.  Your credit card will be charged automatically. We appreciate your business!&lt;/p&gt;
        ///
        ///&lt;p&gt;Questions? Contact us anytime at {1}.&lt;/p&gt;
        ///
        ///&lt;p&gt;------------------------------------
        ///&lt;br/&gt;{0} Invoice - Subscription - {2}
        ///&lt;br/&gt;
        ///&lt;br/&gt;Amount: USD {3}*
        ///&lt;br/&gt;
        ///&lt;br/&gt;Charged to: ({4})
        ///&lt;br/&gt;Date: {6}
        ///&lt;br/&gt;For service through: {7}
        ///&lt;br/&gt;
        ///&lt;br/&gt;{8}
        ///&lt;br/&gt;-----------------------------------&lt;/p&gt;
        ///&lt;p  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string PaymentInvoice_Body {
            get {
                return ResourceManager.GetString("PaymentInvoice_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;We received payment for your {0} subscription. Thanks for your business!&lt;/p&gt;
        ///
        ///&lt;p&gt;Questions? Contact us anytime at {1}.&lt;/p&gt;
        ///
        ///&lt;p&gt;------------------------------------
        ///&lt;br/&gt;{0} Receipt - Subscription - {2}
        ///&lt;br/&gt;
        ///&lt;br/&gt;Amount: USD {3}
        ///&lt;br/&gt;
        ///&lt;br/&gt;Charged to: ({4})
        ///&lt;br/&gt;Transaction ID: {5}
        ///&lt;br/&gt;Date: {6}
        ///&lt;br/&gt;For service through: {7}
        ///&lt;br/&gt;
        ///&lt;br/&gt;{8}
        ///&lt;br/&gt;-----------------------------------&lt;/p&gt;.
        /// </summary>
        public static string PaymentReceipt_Body {
            get {
                return ResourceManager.GetString("PaymentReceipt_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;The {1} pre-review has started. You&apos;ll need to customize reviews for your direct reports by  {2}. You can begin your pre-review customizations by clicking the following link:&lt;/p&gt;
        ///&lt;br&gt; 
        ///    &lt;a href=&quot;{3}&quot;&gt;{4}&lt;/a&gt;
        ///&lt;br /&gt;&lt;br /&gt;If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {5} Team.
        /// </summary>
        public static string Prereview_Body {
            get {
                return ResourceManager.GetString("Prereview_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have a new pre-review task from {0}.
        /// </summary>
        public static string Prereview_Subject {
            get {
                return ResourceManager.GetString("Prereview_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;You have a new Quarterly Conversation due on {1}. You can complete it by clicking the following link:&lt;/p&gt;
        ///&lt;br&gt; 
        ///    &lt;a href=&quot;{2}&quot;&gt;{3}&lt;/a&gt;
        ///&lt;br /&gt;&lt;br /&gt;If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {4} Team.
        /// </summary>
        public static string QuarterlyConversation_Body {
            get {
                return ResourceManager.GetString("QuarterlyConversation_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;You have a new Quarterly Conversation due on {1}. You can complete it by clicking the following link:&lt;/p&gt;
        ///&lt;br&gt; 
        ///    &lt;a href=&quot;{2}&quot;&gt;{3}&lt;/a&gt;
        ///&lt;br /&gt;&lt;br /&gt;If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {4} Team.
        /// </summary>
        public static string QuarterlyConversationReminder_Body {
            get {
                return ResourceManager.GetString("QuarterlyConversationReminder_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;Attached please find the quarterly printout for {0}.&lt;/p&gt;
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Thank you,&lt;br /&gt;
        ///The {1} Team.
        /// </summary>
        public static string QuarterlyPrintout_Body {
            get {
                return ResourceManager.GetString("QuarterlyPrintout_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reminder, you still have a review to complete..
        /// </summary>
        public static string ReminderReview_Subject {
            get {
                return ResourceManager.GetString("ReminderReview_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;You have a review due on {1}. You can take your review by clicking the following link:&lt;/p&gt;
        ///    &lt;a href=&quot;{2}&quot;&gt;{3}&lt;/a&gt;
        ///&lt;br /&gt;&lt;br /&gt;If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {4} Team.
        /// </summary>
        public static string RemindReview_Body {
            get {
                return ResourceManager.GetString("RemindReview_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;p&gt;{0},&lt;/p&gt;
        ///&lt;p&gt;Here are your to-dos. To view your to-dos, click &lt;a href=&apos;{3}&apos;&gt;here&lt;/a&gt;.&lt;/p&gt;
        ///{1}
        ///&lt;br /&gt;If you have any questions, feedback, or concerns, you can reply to this e-mail address.
        ///&lt;br /&gt;
        ///&lt;br /&gt;
        ///Sincerely,&lt;br/&gt;
        ///The {2} Team.
        /// </summary>
        public static string TodoReminder_Body {
            get {
                return ResourceManager.GetString("TodoReminder_Body", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}.
        /// </summary>
        public static string TodoReminder_Subject {
            get {
                return ResourceManager.GetString("TodoReminder_Subject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hello,
        ///
        ///I hope that this email finds you well.
        ///
        ///We noticed that your credit card on file is about to expire. Please update your card information within {0}.
        ///
        ///If you do need to update your card information in your {0} account please follow the steps below:
        ///
        ///1. Log in to your {0} account.
        ///2. Navigate to &quot;Manage Organization&quot; from the drop down menu by your picture (Upper right corner) then click the &quot;Payment&quot; tab (click this link {1}Manage/Payment)
        ///3. Click &quot;Add Bank Account&quot; or “Add Credit Card” bu [rest of string was truncated]&quot;;.
        /// </summary>
        public static string UpdateCard_Body {
            get {
                return ResourceManager.GetString("UpdateCard_Body", resourceCulture);
            }
        }
    }
}