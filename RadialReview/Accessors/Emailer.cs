﻿using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using RadialReview.Utilities.DataTypes;
using Mandrill.Models;
using System.Text.RegularExpressions;
using Mandrill;
using Mandrill.Requests.Messages;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using Hangfire;

namespace RadialReview.Accessors {
	public class EmailResult {
		public int Sent { get; set; }
		public int Unsent { get; set; }
		public int Queued { get; set; }
		public int Total { get; set; }
		public int Faults { get; set; }
		public TimeSpan TimeTaken { get; set; }
		public List<Exception> Errors { get; set; }

		public EmailResult() {
			Errors = new List<Exception>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="successMessage">
		///     {0} = Sent,<br/>
		///     {1} = Unsent,<br/>
		///     {2} = Total,<br/>
		///     {3} = TimeTaken(InSeconds),<br/>
		///     </param>
		/// <returns></returns>
		public ResultObject ToResults(String successMessage) {
			if (Errors.Count() > 0) {
				var message = String.Join(",\n", Errors.Select(x => x.Message).Distinct());
				return new ResultObject(new RedirectException(Errors.Count() + " errors:\n" + message));
			}
			return ResultObject.Create(false, String.Format(successMessage, Sent, Unsent, Total, TimeTaken.TotalSeconds));

		}
	}

	public class Emailer : BaseAccessor {
		private static Regex _regex = CreateRegEx();
		public bool IsValid(object value) {
			if (value == null) {
				return true;
			}

			string valueAsString = value as string;

			// Use RegEx implementation if it has been created, otherwise use a non RegEx version.
			if (_regex != null) {
				return valueAsString != null && _regex.Match(valueAsString).Length > 0;
			} else {
				int atCount = 0;

				foreach (char c in valueAsString) {
					if (c == '@') {
						atCount++;
					}
				}

				return (valueAsString != null
				&& atCount == 1
				&& valueAsString[0] != '@'
				&& valueAsString[valueAsString.Length - 1] != '@');
			}
		}

		private static Regex CreateRegEx() {
			// We only need to create the RegEx if this switch is enabled.


			const string pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
			const RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

			// Set explicit regex match timeout, sufficient enough for email parsing
			// Unless the global REGEX_DEFAULT_MATCH_TIMEOUT is already set
			TimeSpan matchTimeout = TimeSpan.FromSeconds(2);

			try {
				if (AppDomain.CurrentDomain.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") == null) {
					return new Regex(pattern, options, matchTimeout);
				}
			} catch {
				// Fallback on error
			}

			// Legacy fallback (without explicit match timeout)
			return new Regex(pattern, options);
		}





		#region Helpers
		private static String EmailBodyWrapper(String htmlBody, int? tableWidth = null) {
			var footer = String.Format(EmailStrings.Footer, ProductStrings.CompanyName);
			return String.Format(EmailStrings.BodyWrapper, htmlBody, footer, tableWidth ?? 600);
		}

		public static bool IsValid(string emailaddress) {
			if (emailaddress == null)
				return false;
			try {
				MailAddress m = new MailAddress(emailaddress);
				return true;
			} catch (FormatException) {
				return false;
			}
		}


		/// <summary>
		/// Display's a link with or without query parameters.
		/// </summary>
		/// <returns></returns>
		public static string ReplaceLink(string text,bool hideQueryParams=false) {
			if (!hideQueryParams)
			return Regex.Replace(text,
				@"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
				"<a target='_blank' href='$1'>$1</a>");

			return Regex.Replace(text,
				@"(((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@^=%&amp;:/~\+#]*[\w\-\@^=%&amp;/~\+#])?)([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
				"<a target='_blank' href='$1'>$2</a>"
			);
		}


		#endregion
		#region AsyncMailer

		//private static MailMessage CreateMessage(EmailModel email)
		//{
		//    MailMessage message = new MailMessage()
		//    {
		//        Subject = email.Subject,
		//        Body = email.Body,
		//        IsBodyHtml = true,
		//        From = new MailAddress(ConstantStrings.SmtpFromAddress),
		//    };
		//    message.To.Add(email.ToAddress);
		//    return message;
		//}

		private static Pool<SmtpClient> SmtpPool = new Pool<SmtpClient>(30, TimeSpan.FromMinutes(2), () => new SmtpClient {
			Host = ConstantStrings.SmtpHost,
			Port = int.Parse(ConstantStrings.SmtpPort),
			Timeout = 50000,
			EnableSsl = true,
			Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword)
		});
		/*
		private static async Task<int> SendEmailsFast(List<EmailModel> emails, EmailResult result)
		{
			SemaphoreSlim throttler = new SemaphoreSlim(3);
			var allTasks = new List<Task>();
			try
			{
				foreach (var email in emails)
				{        
					await throttler.WaitAsync();
                    
					allTasks.Add(Task.Run(async () =>
					{
						try
						{
							var errCount = 0;
							var maxError = 5;
							while (errCount<maxError)
							{
								await Task.Delay(100);
								var smtpClient = SmtpPool.GetObject();
								try{
									smtpClient.Send(CreateMessage(email));
									lock (result){result.Sent += 1;}
									SmtpPool.PutObject(smtpClient);
									break;
								}catch (Exception e){
									errCount++;
									if (errCount == maxError){
										log.Error("Couldnt sent mail (" + email.Id + ")", e);
										lock (result){
											result.Unsent += 1;
											result.Errors.Add(e);
										}
										SmtpPool.DisposeObject(smtpClient);
										return false;
									}else{
										SmtpPool.DisposeObject(smtpClient);
									}
								}
							}
							return true;
						}
						finally
						{
							throttler.Release();
						}
					}));
				}

				await Task.WhenAll(allTasks);


				/*await Task.WhenAll(emails.Select(email =>{
					maxTask.Wait();
					var output =Task.Run(async () =>
						return true;
					});
					maxTask.Release();
					return output;
				}));*
			}
			catch (Exception e)
			{
				log.Error("All emails failed", e);
				result.Errors.Add(e);
			}
			return SmtpPool.Available();
		}*/

		//private static void Complete(object o, AsyncCompletedEventArgs e)
		//{
		//    int a = 0;

		//}

		//private static async Task<int> SendEmailsFast(List<EmailModel> emails, EmailResult result)
		//{
		//    /*var allTasks = new List<Task>();
		//    foreach (var email in emails)
		//    {
		//        SmtpPool.GetObject()

		//        allTasks.Add(Task.Run(async () =>
		//        {
		//            try
		//            {
		//                var errCount = 0;
		//                var maxError = 5;
		//                while (errCount < maxError)
		//                {
		//                    await Task.Delay(100);
		//                    var smtpClient = SmtpPool.GetObject();
		//                    try
		//                    {
		//                        smtpClient.Send(CreateMessage(email));
		//                        lock (result) { result.Sent += 1; }
		//                        SmtpPool.PutObject(smtpClient);
		//                        break;
		//                    }
		//                    catch (Exception e)
		//                    {
		//                        errCount++;
		//                        if (errCount == maxError)
		//                        {
		//                            log.Error("Couldnt sent mail (" + email.Id + ")", e);
		//                            lock (result)
		//                            {
		//                                result.Unsent += 1;
		//                                result.Errors.Add(e);
		//                            }
		//                            SmtpPool.DisposeObject(smtpClient);
		//                            return false;
		//                        }
		//                        else
		//                        {
		//                            SmtpPool.DisposeObject(smtpClient);
		//                        }
		//                    }
		//                }
		//                return true;
		//            }
		//            finally
		//            {
		//                throttler.Release();
		//            }
		//        }));
		//    }*/


		//    await Task.WhenAll(emails.Select(async email =>
		//    {
		//        var errors = 0;
		//        while (true)
		//        {
		//            var smtp = await SmtpPool.GetObject();
		//            try
		//            {
		//                await smtp.SendMailAsync(CreateMessage(email));
		//                lock (result)
		//                {
		//                    result.Sent += 1;
		//                }
		//                SmtpPool.PutObject(smtp);
		//                return true;
		//            }
		//            catch (Exception e)
		//            {
		//                errors++;
		//                SmtpPool.DisposeObject(smtp);
		//                lock (result){
		//                    result.Faults += 1;
		//                }

		//                if (errors == 5){
		//                    lock (result){
		//                        result.Unsent += 1;
		//                        result.Errors.Add(e);
		//                    }
		//                    break;
		//                }
		//            }
		//            await Task.Delay(1000);
		//        }
		//        return false;
		//    })
		//    );
		//    return 1;

		//}




		public class MandrillModel {
			public String FirstName { get; set; }
			public String LastName { get; set; }
		}

		private static string FixEmail(string email) {
			return Config.FixEmail(email);


		}

		private static SendMessageRequest CreateMandrillMessageRequest(EmailModel email) {
			var message = CreateMandrillMessage(email);
			return new SendMessageRequest(message);
		}

		private static EmailMessage CreateMandrillMessage(EmailModel email) {
			var toAddress = FixEmail(email.ToAddress);

			var toAddresses = new EmailAddress(toAddress).AsList();
			if (email.Bcc != null) {
				foreach (var bcc in email.Bcc.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					var fixedBcc = FixEmail(bcc);
					toAddresses.Add(new EmailAddress(fixedBcc) { Type = "bcc" });
				}
			}

			var body = email.Body;
			if (email._TrackerId != null) {
				try {
					body += "<br/><img src='" + Config.BaseUrl(null) + "t/mark/" +  email._TrackerId + "'/>";
				} catch (Exception e) {
					//ops
				}
			}

			var oEmail = new EmailMessage() {
				FromEmail = MandrillStrings.FromAddress,
				FromName = email._ReplyToName ?? MandrillStrings.FromName,
				Html = body,
				Subject = email.Subject,
				To = toAddresses,
				TrackOpens = true,
				TrackClicks = true,
				GoogleAnalyticsDomains = Config.GetMandrillGoogleAnalyticsDomain().NotNull(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()),
			};

			if (email._Attachments != null && email._Attachments.Any()) {
				oEmail.Attachments = email._Attachments;
			}

			if (!string.IsNullOrWhiteSpace(email._ReplyToEmail)) {
				oEmail.AddHeader("Reply-To", email._ReplyToEmail);
			}


			return oEmail;
		}

		private static async Task<List<Mandrill.Models.EmailResult>> SendMessage(MandrillApi api, EmailModel email) {
			var result = await api.SendMessage(CreateMandrillMessageRequest(email));
			email.MandrillId = result.FirstOrDefault().NotNull(x => x.Id);
			return result;
		}

		private static async Task<int> SendMandrillEmails(List<EmailModel> emails, EmailResult result, bool forceSend = false) {

			var api = new MandrillApi(ConstantStrings.MandrillApiKey, true);
			var results = new List<Mandrill.Models.EmailResult>();

			if (!emails.Any())
				return 1;
			if (Config.SendEmails() || forceSend) {
				results = (await Task.WhenAll(emails.Select(email => SendMessage(api, email)))).SelectMany(x => x).ToList();
			} else {
				results = emails.Select(x => new Mandrill.Models.EmailResult() {
					Status = EmailResultStatus.Sent,
					Email = x.ToAddress,
				}).ToList();
			}
			var now = DateTime.UtcNow;
			foreach (var r in results) {
				switch (r.Status) {
					case EmailResultStatus.Invalid: {
							result.Unsent += 1;
							result.Errors.Add(new Exception("Invalid"));
							break;
						}
					case EmailResultStatus.Queued:
						result.Queued += 1;
						break;
					case EmailResultStatus.Rejected: {
							result.Unsent += 1;
							result.Errors.Add(new Exception(r.RejectReason));
							break;
						}
					case EmailResultStatus.Scheduled:
						result.Queued += 1;
						break;
					case EmailResultStatus.Sent: {
							result.Sent += 1;
							try {
								var found = emails.First(x => x.ToAddress.ToLower() == r.Email.ToLower());
								found.Sent = true;
								found.CompleteTime = now;
							} catch (Exception) {
							}
						}
						break;
					default:
						break;
				}
			}


			return 1;
		}


		[Queue(HangfireQueues.Immediate.EXECUTE_TASKS)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task EnqueueEmail(Mail email, bool wrapped=true) {
			Scheduler.Enqueue(() => Emailer.SendEmail(email, false, wrapped));
		}
		
		public static async Task<EmailResult> SendEmail(Mail email, bool forceSend = false, bool wrapped = true) {
			return await SendEmails(email.AsList(), forceSend: forceSend, wrapped: wrapped);
		}

		public static async Task<EmailResult> SendEmails(IEnumerable<Mail> emails, bool forceSend = false, bool wrapped = true) {
			return await SendEmailsWrapped(emails, forceSend: forceSend, wrapped: wrapped);
		}

		private static async Task<EmailResult> SendEmailsWrapped(IEnumerable<Mail> emails, bool forceSend = false, int? tableWidth = null, bool wrapped = true) {
			//Register emails
			var unsentEmails = new List<EmailModel>();
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var email in emails) {
						var unsent = new EmailModel() {
							Body = wrapped ? EmailBodyWrapper(email.HtmlBody, tableWidth) : email.HtmlBody,
							CompleteTime = null,
							Sent = false,
							Subject = email.Subject,
							ToAddress = email.ToAddress,
							Bcc = String.Join(",", email.BccList),
							SentTime = now,
							EmailType = email.EmailType,
							_ReplyToName = email.ReplyToName,
							_ReplyToEmail = email.ReplyToAddress,
							_TrackerId = email.TrackerId,
						};

						if (email.Attachment != null) {
							unsent._Attachments = new List<EmailAttachment>() {
								email.Attachment
							};
						}


						s.Save(unsent);
						unsentEmails.Add(unsent);
					}
					tx.Commit();
					s.Flush();
				}
			}

			var result = new EmailResult() { Total = unsentEmails.Count };
			//Now send everything
			var startSending = DateTime.UtcNow;

			//And... Go.
			var threads = await SendMandrillEmails(unsentEmails, result, forceSend);

			result.TimeTaken = DateTime.UtcNow - startSending;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var email in unsentEmails) {
						s.Update(email);
					}
					tx.Commit();
					s.Flush();
				}
			}

			return result;
		}



		#endregion

		#region oldSyncMailer
		/*
        private static void SendEmail(String address, String subject, String htmlHody, int emailId)
        {
            // ""
            // "smtp.gmail.com"
            // 587                



            MailMessage message = new MailMessage
            {
                Subject = subject,
                Body = htmlHody,
                IsBodyHtml = true,
                From = new MailAddress(ConstantStrings.SmtpFromAddress),
            };
            message.To.Add(address);
            SmtpClient SmtpMailer = new SmtpClient
            {
                Host = ConstantStrings.SmtpHost,
                Port = int.Parse(ConstantStrings.SmtpPort),
                Timeout = 50000,
                EnableSsl = true
            };
            SmtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);
            SmtpMailer.SendCompleted += EmailComplete;
            SmtpMailer.SendAsync(message, emailId);
        }

        private static void SendEmailSync(String address, String subject, String body, EmailModel email)
        {
            MailMessage message = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                From = new MailAddress(ConstantStrings.SmtpFromAddress),
            };
            message.To.Add(address);
            SmtpClient SmtpMailer = new SmtpClient
            {
                Host = ConstantStrings.SmtpHost,
                Port = int.Parse(ConstantStrings.SmtpPort),
                Timeout = 50000,
                EnableSsl = true
            };
            SmtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);
            SmtpMailer.Send(message);

            email.Sent = true;
            email.CompleteTime = DateTime.UtcNow;
        }
        
        private static void EmailComplete(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var email = s.Get<EmailModel>(e.UserState);
                        email.Sent = true;
                        email.CompleteTime = DateTime.UtcNow;
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            /*
            using (var db = new ApplicationDbContext())
            {
                var email=db.Emails.Find(e.UserState);
                db.SaveChanges();
            }*
        }*/
		/*
		private static async Task SendMailAsync(SmtpClient smtpMailer,MailModel email)
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			SendCompletedEventHandler handler = null;
			handler = delegate(object sender, AsyncCompletedEventArgs e)
			{
				smtpMailer.HandleCompletion(tcs, e, handler);
			};
			smtpMailer.SendCompleted += handler;
			try
			{
				this.SendAsync(message, tcs);
			}
			catch
			{
				this.SendCompleted -= handler;
				throw;
			}
			return tcs.Task;
		}

		public async static Task SendEmails(List<MailModel> emails)
		{
			SmtpClient smtpMailer = new SmtpClient
			{
				Host = ConstantStrings.SmtpHost,
				Port = int.Parse(ConstantStrings.SmtpPort),
				Timeout = 50000,
				EnableSsl = true
			};
			smtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);

			var tasks =emails.Select(x=>smtpMailer.SendMailAsync(

			Task.WhenAll(tasks);

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					tx.Commit();
					s.Flush();
				}
			}
		}*/
		/*
		public static void SendEmail(String toAddress, String subject, String htmlBody)
		{
			if (!IsValid(toAddress))
				throw new RedirectException(ExceptionStrings.InvalidEmail);

			var body = EmailBodyWrapper(htmlBody);
			long emailId = -1;
			using (var s = HibernateSession.GetCurrentSession())
			{
				EmailModel email;
				using (var tx = s.BeginTransaction())
				{
					email = new EmailModel()
								{
									Body = body,
									Sent = false,
									SentTime = DateTime.UtcNow,
									Subject = subject,
									ToAddress = toAddress
								};
					s.Save(email);
					//db.SaveChanges();
					emailId = email.Id;

					SendEmailSync(toAddress, subject, body, email);
					s.Update(email);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void SendEmail(AbstractUpdate s, String toAddress, String subject, String htmlBody)
		{
			if (!IsValid(toAddress))
				throw new RedirectException(ExceptionStrings.InvalidEmail);

			var body = EmailBodyWrapper(htmlBody);
			long emailId = -1;
			try
			{
				var email = new EmailModel()
				{
					Body = body,
					Sent = false,
					SentTime = DateTime.UtcNow,
					Subject = subject,
					ToAddress = toAddress
				};
				s.Save(email);


				//db.SaveChanges();
				emailId = email.Id;

				SendEmailSync(toAddress, subject, body, email);
				s.Update(email);
			}
			catch (Exception e)
			{
				log.Error(e);
			}

			//SendEmail(toAddress, subject, body, emailId);

			/*MailMessage message = new MailMessage
			{
				Subject = subject,
				Body = body,
				IsBodyHtml = true,
				From = new MailAddress(ConstantStrings.SmtpFromAddress),
			};
			message.To.Add(toAddress);
			SmtpClient SmtpMailer = new SmtpClient
			{
				Host = ConstantStrings.SmtpHost,
				Port = int.Parse(ConstantStrings.SmtpPort),
				Timeout = 50000,
				EnableSsl = true
			};
			SmtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);
			SmtpMailer.Send(message);

			email.Sent = true;
			email.CompleteTime = DateTime.UtcNow;
			s.Update(email);*
		}*/
		#endregion
	}

}