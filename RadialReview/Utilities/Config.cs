﻿using Amazon.Util;
using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities.Constants;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

namespace RadialReview.Utilities {
	public class Config {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void DbUpdateSuccessful() {
			var env = GetEnv();
			if (env == Env.production) {
				return;
			}

			var version = GetAppSetting("dbVersion", "0");
			var dir = Path.Combine(Path.GetTempPath(), "Elite");

			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			var file = Path.Combine(dir, "dbversion" + env + ".txt");
			if (!File.Exists(file)) {
				File.CreateText(file).Close();
			}

			while (FileUtilities.IsFileLocked(new FileInfo(file))) {
				Thread.Sleep(100);
			}
			File.WriteAllText(file, version);
		}

		public static bool IsTest() {
			return GetEnv() == Env.local_test_sqlite;
		}

		public static bool RunChromeExt() {
			switch (GetEnv()) {
				case Env.local_test_sqlite:
					return true;
				case Env.local_sqlite:
					return false;
				case Env.local_mysql:
					return GetAppSetting("RunExt", "false").ToBooleanJS();
				case Env.production:
					return false;
				case Env.dev_testing:
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public class DocumentRepoSettings {
			public string Bucket { get; set; }
			public string Path { get; set; }
		}

		public static DocumentRepoSettings GetDocumentRepositorySettings() {
			return new DocumentRepoSettings() {
				Bucket = GetAppSetting("DocumentRepo_Bucket", "elitetools"),
				Path = GetAppSetting("DocumentRepo_Path", "DocumentRepo"),
			};
		}

		public enum DbType {
			Invalid = 0,
			MySql = 1,
			Sqlite = 2
		}

		public static string[] GetCookieDomains() {
			if (IsLocal()) {
				return new string[0];
			}

			var res = GetAppSetting("CookieDomains", null);
			if (string.IsNullOrWhiteSpace(res)) {
				return new string[0];
			}

			return res.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim())
				.ToArray();
		}

		public static DbType GetDatabaseType() {
			switch (GetEnv()) {
				case Env.local_test_sqlite:
					return DbType.Sqlite;
				case Env.local_sqlite:
					return DbType.Sqlite;
				case Env.local_mysql:
					return DbType.MySql;
				case Env.production:
					return DbType.MySql; ////
				case Env.dev_testing:
					return DbType.MySql;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static int EnterpriseAboveUserCount() {
			return 45;
		}

		public class ActiveCampaignConfig {
			public string BaseUrl { get; set; }
			public string EventUrl { get; set; }
			public string ApiKey { get; set; }

			/// <summary>
			/// https://REPLACE_ME.activehosted.com/admin/main.php?action=settings#tab_track  >> click "Event Tracking API"
			/// </summary>
			public string ActId { get; set; }
			/// <summary>
			/// "Event Key" https://REPLACE_ME.activehosted.com/admin/main.php?action=settings#tab_track
			/// </summary>
			public string TrackKey { get; set; }

			public bool TestMode { get; set; }
			public ConfigFields Fields { get; set; }
			public ConfigLists Lists { get; set; }

			public class ConfigFields {
				public long Autogenerated { get; internal set; }
				public long OrgId { get; internal set; }

				/// <summary>
				/// Business Coach",
				///	Certified of Professional EOS Implementer",
				///	Other",
				///	Unknown",
				///	EOS Implementer (Basecamp only; no certs)",
				/// </summary>
				public long CoachType { get; internal set; }
				public long CoachName { get; internal set; }
				public long AssignedTo { get; internal set; }
				public long AccountType { get; internal set; }
				public long HasEosImplementer { get; internal set; }
				public long UserId { get; internal set; }
				public long IsTest { get; internal set; }
				public long ReferralSource { get; internal set; }
				public long ReferralQuarter { get; internal set; }
				public int ReferralYear { get; internal set; }
				public long Title { get; internal set; }
				public long CoachLastReferral { get; internal set; }
				public long CoachHasReferral { get; internal set; }
				public long TrialEnd { get; internal set; }
				public long TrialStart { get; internal set; }
			}
			public class ConfigLists {
				public long PrimaryContact { get; internal set; }
				public long Implementer { get; internal set; }
				public long ContactList { get; internal set; }
				public long CoachThatReferred { get; internal set; }
			}

		}

		public class AgileCrmConfig {
			public string Domain { get; set; }
			public string Email { get; set; }
			public string CrmKey { get; set; }
			public bool TestMode { get; set; }
		}

		public static bool UploadFiles() {
			if (IsLocal()) {
				return GetAppSetting("UploadLocalFiles", "false").ToBooleanJS();
			}
			return true;

		}

		public static string GetHangfireConnectionString() {
			switch (GetEnv()) {
				case Env.invalid:
					break;
				case Env.local_sqlite:
					break;
				case Env.local_mysql:
					break;
				case Env.local_test_sqlite:
					break;
				case Env.dev_testing:
					return System.Configuration.ConfigurationManager.ConnectionStrings["ProductionHangfireConnection_dev"].ConnectionString;

				case Env.production:
					return System.Configuration.ConfigurationManager.ConnectionStrings["ProductionHangfireConnection"].ConnectionString;
				default:
					break;
			}
			return System.Configuration.ConfigurationManager.ConnectionStrings["LocalHangfireConnection"].ConnectionString;
		}

		[Obsolete("Must remove when ready for production")]
		public static void ThrowNotImplementedOnProduction() {
			if (!IsLocal()) {
				throw new NotImplementedException();
			}
		}

		public static long TextInNumber() {
			return 1;//REPLACE_ME with Twilio phone number
		}

		public static string ModifyEmail(string email) {
			if (IsLocal()) {
				//REPLACE_ME
				return "rcisney+" + (email ?? "").Replace("@", "_") + "@dlpre.com";
			}
			return email;
		}

		public static ActiveCampaignConfig GetActiveCampaignConfig() {
			var shouldRun = !Config.IsLocal();
			var forceRun = GetAppSetting("ActiveCampaign_ForceRun", "false").ToBooleanJS();
			if (!shouldRun) {
				shouldRun = shouldRun || forceRun;
			}
			var testMode = !shouldRun;

			return new ActiveCampaignConfig() {
				BaseUrl = GetAppSetting("ActiveCampaign_BaseUrl", ""),
				EventUrl = GetAppSetting("ActiveCampaign_EventUrl", ""),
				ApiKey = GetAppSetting("ActiveCampaign_ApiKey", ""),
				ActId = GetAppSetting("ActiveCampaign_ActId", ""),
				TrackKey = GetAppSetting("ActiveCampaign_TrackKey", ""),
				TestMode = testMode,
				Fields = new ActiveCampaignConfig.ConfigFields() {
					HasEosImplementer = 1,
					AccountType = 2,
					AssignedTo = 3,
					UserId = 11,
					Title = 13,
					CoachName = 16,
					CoachType = 17,
					ReferralSource = 20,
					ReferralYear = 22,
					ReferralQuarter = 23,
					OrgId = 30,
					Autogenerated = 32,
					IsTest = 37,
					CoachLastReferral = 42,
					CoachHasReferral = 43,
					TrialEnd = 44,
					TrialStart = 45,
				},
				Lists = new ActiveCampaignConfig.ConfigLists() {
					ContactList = 3,
					Implementer = 4,
					PrimaryContact = 5,
					CoachThatReferred = 6,
				}
			};
		}

		public static string GetSlackNotificationsChannel() {
			var channel = GetAppSetting("SlackNotificationsChannel", null);
			if (channel == "tt-notification" && GetEnv() != Env.production) {
				return null;
			}
			return channel;
		}
		public static AgileCrmConfig GetAgileCrmConfig() {
			var shouldRun = !Config.IsLocal();
			var forceRun = GetAppSetting("AgileCrm_ForceRun", "false").ToBooleanJS();
			if (!shouldRun) {
				shouldRun = shouldRun || forceRun;
			}
			var testMode = !shouldRun;

			return new AgileCrmConfig() {
				Domain = GetAppSetting("AgileCrm_Domain", ""),
				Email = GetAppSetting("AgileCrm_Email", ""),
				CrmKey = GetAppSetting("AgileCrm_CrmKey", ""),
				TestMode = testMode,
			};
		}

		public static string SchedulerSecretKey() {
			var found = GetAppSetting("Scheduler_SecretKey", null);
			if (string.IsNullOrWhiteSpace(found)) {
				throw new Exception("Scheduler Key not found in file.");
			}

			return found;
		}

		public static string UpdateTaskUserName() {
			var found = GetAppSetting("UpdateTaskUserName", null);
			if (string.IsNullOrWhiteSpace(found)) {
				throw new Exception("UpdateTask UserName not found in file.");
			}

			return found;
		}

		public static string GetUpdateTaskUrl() {
			var url = "http://localhost:44300/coreprocess/process/UpdateTasks";
			if (!IsLocal()) {
				url = GetAppSetting("UpdateTaskUrl", null);
				if (string.IsNullOrWhiteSpace(url)) {
					throw new Exception("UpdateTaskUrl not found in file.");
				}
			}
			return url;
		}

		public static string GetScedulerHostUrl() {
			var url = "http://localhost:44300/token";
			if (!IsLocal()) {
				url = GetAppSetting("HostName", null);
				if (string.IsNullOrWhiteSpace(url)) {
					throw new Exception("HostName not found in file.");
				}
			}
			return url;
		}

		public static string GetSQSQueueURL() {
			var url = GetAppSetting("SQS_QueueURL", null);
			if (string.IsNullOrWhiteSpace(url)) {
				throw new Exception("SQS_QueueURL not found in file.");
			}

			return url;
		}

		public static string GetSQSAccessKey() {
			var access_key = GetAppSetting("SQS_AccessKey", null);
			if (string.IsNullOrWhiteSpace(access_key)) {
				throw new Exception("SQS_AccessKey not found in file.");
			}

			return access_key;
		}

		public static string GetSQSSecretKey() {
			var secret_key = GetAppSetting("SQS_SecretKey", null);
			if (string.IsNullOrWhiteSpace(secret_key)) {
				throw new Exception("SQS_SecretKey not found in file.");
			}

			return secret_key;
		}


		public class InternalZapierEndpointType {



			public static readonly InternalZapierEndpointType UserOrganizationCreate = new InternalZapierEndpointType("UserOrganizationCreate", "https://hooks.zapier.com/hooks/catch/REPLACE_ME1/");
			public static readonly InternalZapierEndpointType UserOrganizationAttach = new InternalZapierEndpointType("UserOrganizationAttach", "https://hooks.zapier.com/hooks/catch/REPLACE_ME1/");
			public static readonly InternalZapierEndpointType UserOrganizationUpdate = new InternalZapierEndpointType("UserOrganizationUpdate", "https://hooks.zapier.com/hooks/catch/REPLACE_ME1/");
			public static readonly InternalZapierEndpointType UserUpdate = new InternalZapierEndpointType("UserUpdate", "https://hooks.zapier.com/hooks/catch/REPLACE_ME1/");

			public static readonly InternalZapierEndpointType OrganizationCreate = new InternalZapierEndpointType("OrganizationCreate", "https://hooks.zapier.com/hooks/catch/REPLACE_ME2/");
			public static readonly InternalZapierEndpointType OrganizationUpdate = new InternalZapierEndpointType("OrganizationUpdate", "https://hooks.zapier.com/hooks/catch/REPLACE_ME2/");

			public static readonly InternalZapierEndpointType PaymentUpdate = new InternalZapierEndpointType("PaymentUpdate", "https://hooks.zapier.com/hooks/catch/REPLACE_ME3/");

			public static readonly InternalZapierEndpointType AccountCharged = new InternalZapierEndpointType("AccountCharged", null);


			public string Type { get; set; }
			[Obsolete("do not use")]
			public string DefaultUrl { get; set; }

			private InternalZapierEndpointType(string type, string defaultUrl) {
				Type = type;
				DefaultUrl = defaultUrl;
			}
			public string GetDefaultUrl() {
				return DefaultUrl;
			}

			[Obsolete("do not use")]
			public InternalZapierEndpointType() {
			}

		}

		public static string GetInternalZapierEndpoint(InternalZapierEndpointType type) {
			if (Config.IsLocal()) {
				return null;
			}

			if (type == null) {
				return null;
			}

			return GetAppSetting("zapier_internal_endpoint_" + type.Type, type.GetDefaultUrl());
		}

		//public static string PeopleAreaName() {
		//    return "People";
		//}


		public static bool ShouldUpdateDB(string method) {
			var version = GetAppSetting("dbVersion", "0");
			if (version == "0") {
				return true;
			}

			var env = GetEnv();

			switch (env) {
				case Env.local_test_sqlite:
					return true;
				case Env.local_mysql:
					goto case Env.local_sqlite;
				case Env.local_sqlite: {
						var dir = Path.Combine(Path.GetTempPath(), "elite");
						var file = Path.Combine(dir, "dbversion" + env + ".txt");
						if (!Directory.Exists(dir)) {
							Directory.CreateDirectory(dir);
						}

						if (!File.Exists(file)) {
							File.Create(file);
							while (!File.Exists(file)) {
								Thread.Sleep(100);
							}
							Thread.Sleep(100);
						}
						var i = 0;
						while (true) {
							try {
								if (version == File.ReadAllText(file)) {
									return false;
								} else {
									return true;
								}
							} catch (Exception) {
								Thread.Sleep(100);
							}
							i++;
							if (i > 20) {
								throw new Exception("File in use");
							}
						}
						//						return true;
					}
				case Env.production: {
						return ShouldUpdateDB_Production(method);
					}
				case Env.dev_testing:
					return true;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static bool ShouldUpdateDB_Production(string method) {
			var prefix = "[ShouldUpdateDB] (" + method + ") ";
			try {
				log.Info(prefix + "Starting.");

				var myInstanceId = EC2InstanceMetadata.InstanceId.ToString();
				log.Info(prefix + "InstanceId: " + myInstanceId);
				var version = AwsUtil.GetVersionLabelGivenInstanceId(myInstanceId);
				log.Info(prefix + "Version: " + version);

				var key = SettingsAccessor.SettingsKey.DatabaseUpdate(version, method);
				var found = SettingsAccessor.GetProductionSetting(key);
				if (found == null) {
					log.Info(prefix + "Executing - First database migration.");
					SettingsAccessor.SetProductionSetting(key, "started-" + DateTime.UtcNow.ToJsMs());
					return true;
				}
				log.Info(prefix + "Not Executing - Already executed.");
				return false;

			} catch (Exception e) {
				//welp...
				log.Error(prefix + "Executing (Fallback) - Exception encountered:", e);
				return true;
			}

		}

		public static string BaseUrl(OrganizationModel organization, string append = null, string subdomain = null) {
			var baseUrl = new Func<string>(() => {

				subdomain = subdomain.NotNull(x => x.TrimEnd('.') + ".") ?? "";

				switch (GetEnv()) {
					case Env.local_sqlite:
						return $"http://{subdomain}localhost:3751/";
					case Env.local_mysql:
						return $"http://{subdomain}localhost:3751/";
					case Env.production:
						if (organization == null) {
							return $"https://{subdomain}dlptools.com/";
						}

						switch (organization.Settings.Branding) {
							case BrandingType.RadialReview:
								return $"https://{subdomain}dlptools.com/";
							case BrandingType.RoundTable:
								return $"https://{subdomain}dlptools.com/";
							default:
								throw new ArgumentOutOfRangeException();
						}
					case Env.local_test_sqlite:
						return $"http://{subdomain}localhost:3751/";
					case Env.dev_testing:
						return $"http://{subdomain}dlptools.elasticbeanstalk.com/";
					default:
						throw new ArgumentOutOfRangeException();
				}
			});
			return baseUrl() + (append ?? "").TrimStart('/');
		}

		public static string GetPQWUrl() {

			return BaseUrl(null, null, "pqw");
			//string pqw = HttpContext.Current.Request.Url.Scheme + "://pqw." + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/') + "/";
			//return pqw;
			//var baseUrl = new Func<string>(() => {                
			//    switch (GetEnv())
			//    {
			//        case Env.local_sqlite:
			//            return "http://pqw.localhost:44302/";
			//        case Env.local_mysql:
			//            return "http://pqw.localhost:44302/";
			//        case Env.production:
			//            return "https://pqw.traction.tools/";
			//        case Env.local_test_sqlite:
			//            return "http://pqw.localhost:44302/";
			//        case Env.dev_testing:                        
			//            return "https://pqw.env-tractiontools-qa-1.us-west-2.elasticbeanstalk.com/";                        
			//        default:
			//            throw new ArgumentOutOfRangeException();
			//    }
			//});
			//return baseUrl().TrimStart('/');
		}

		public static string ProductName(OrganizationModel organization = null) {
			var org = organization.NotNull(x => x);
			if (org != null) {
				switch (org.Settings.Branding) {
					case BrandingType.RadialReview:
						return GetAppSetting("ProductName_Review", "Elite Tools");
					case BrandingType.RoundTable:
						return GetAppSetting("ProductName_Roundtable", "Elite Tools");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			try {
				if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview")) {
					return GetAppSetting("ProductName_Review", "Elite Tools");
				} else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable")) {
					return GetAppSetting("ProductName_Roundtable", "Elite Tools");
				}
			} catch (Exception) {
				//Fall back...
			}
			return GetAppSetting("ProductName_Review", "Elite Tools");
		}

		public static string DirectReportName() {
			return "Direct Report";
		}

		public static string ReviewName(OrganizationModel organization = null) {
			if (organization != null) {
				switch (organization.Settings.Branding) {
					case BrandingType.RadialReview:
						return GetAppSetting("ReviewName_Review", "Eval");
					case BrandingType.RoundTable:
						return GetAppSetting("ReviewName_Roundtable", "Eval");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			try {
				if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview")) {
					return GetAppSetting("ReviewName_Review", "Eval");
				} else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable")) {
					return GetAppSetting("ReviewName_Roundtable", "Eval");
				}
			} catch (Exception) {
				//Fall back...
			}
			return GetAppSetting("ReviewName_Review", "Eval");
		}


		public static KeyManager.DatabaseCredentials GetEnvironmentRDS() {

			return new KeyManager.DatabaseCredentials() {
				DatabaseIdentifier = "EnvironmentRDS",
				Host = GetAppSetting("RDS_HOSTNAME"),
				Port = GetAppSetting("RDS_PORT"),
				Password = GetAppSetting("RDS_PASSWORD"),
				Username = GetAppSetting("RDS_USERNAME"),
				Database = GetAppSetting("RDS_DB_NAME"),
			};
		}

		public static string ManagerName() {
			return "Supervisor";
		}

		public class CamundaCredentials {
			public string Url { get; set; }
			public string Username { get; set; }
			public string Password { get; set; }
			public bool IsLocal { get; set; }
		}

		public static CamundaCredentials GetCamundaServer() {
			CamundaCredentials credentials = new CamundaCredentials();
			credentials.Username = "demo";
			credentials.Password = "demo";

			switch (GetEnv()) {
				case Env.local_test_sqlite:
					credentials.Url = "http://localhost:8080/engine-rest";
					credentials.IsLocal = true;
					return credentials;
				case Env.local_mysql:
					credentials.Url = "http://localhost:8080/engine-rest";
					credentials.IsLocal = true;
					return credentials;
				case Env.production:
					credentials.IsLocal = false;
					credentials.Url = GetAppSetting("Camunda_Url");
					credentials.Username = GetAppSetting("Camunda_Username");
					credentials.Password = GetAppSetting("Camunda_Password");
					return credentials;
				case Env.dev_testing:
					credentials.Url = "http://localhost:8080/engine-rest";
					credentials.IsLocal = true;
					return credentials;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public class Mock {
			public class LocalMock : IDisposable {
				public void Dispose() {
					Config.MockIsLocal = null;
				}
			}

			public static IDisposable MockLocal(bool isLocal) {
				MockIsLocal = isLocal;
				return new LocalMock();
			}

		}

		private static bool? MockIsLocal = null;

		public static bool IsLocal() {
			if (MockIsLocal != null) {
				return MockIsLocal.Value;
			}

			switch (GetEnv()) {
				case Env.local_test_sqlite:
					return true;
				case Env.local_sqlite:
					return true;
				case Env.local_mysql:
					return true;
				case Env.production:
					return false;
				case Env.dev_testing:
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static bool ShouldDeploy() {
			return !IsLocal();
		}

		public static bool IsSchedulerAction() {
			if (!IsLocal()) {
				if (GetAppSetting("SchedulerAction", "false").ToBooleanJS()) {
					return true;
				}
			}
			return false;
		}

		public static string GetAppSetting(string key, string deflt = null) {
			var config = System.Configuration.ConfigurationManager.AppSettings;
			return config[key] ?? deflt;
		}

		public static string FixEmail(string email) {
			if (Config.IsLocal()) {
				if (string.IsNullOrWhiteSpace(email))
					return "";
				string appSettingEmail = GetAppSetting("TestEmail", "rcisney+test_{0}@dlpre.com");
				return String.Format(appSettingEmail, ((email ?? "").Replace("@", "_at_")));
			} else {
				return email;
			}
		}

		public static Env GetEnv() {
			Env result;
			var env = GetAppSetting("Env");
			if (env != null && Enum.TryParse(env.ToLower(), out result)) {
				return result;
			}
			return Env.local_mysql;
			//throw new Exception("Invalid Environment");
		}

		public static bool IsDefinitelyAlpha() {
			return GetVersion() == ApplicationVersion.alpha;
		}

		public static bool IsDefinitelyQa() {
			return GetVersion() == ApplicationVersion.qa;
		}
		public static bool IsDefinitelyDev() {
			return GetVersion() == ApplicationVersion.dev;
		}
		public static bool IsDefinitelyNonproducitonEnvironment() {
			return IsDefinitelyQa() || IsDefinitelyDev() ;
		}

		public static bool IsHangfireWorker() {
			if (Config.IsLocal() || IsDefinitelyQa() || IsDefinitelyDev()) {
				return true;
			}

			return GetAppSetting("IsHangfireWorker", "false").ToBooleanJS();
		}

		public static string GetAwsEnv() {
			return GetAppSetting("AwsEnv", "na");
		}

		public static ApplicationVersion GetVersion() {
			ApplicationVersion version = ApplicationVersion.invalid;
			if (Enum.TryParse(GetAppSetting("ApplicationVersion", "" + ApplicationVersion.invalid).ToLower(), out version)) {
			}
			return version;
		}

		public static string GetSecret() {
			return GetAppSetting("sha_secret");
		}

		public static bool OptimizationEnabled() {
			switch (GetEnv()) {
				case Env.local_sqlite:
					return GetAppSetting("OptimizeBundles", "False").ToBoolean();
				case Env.local_mysql:
					return GetAppSetting("OptimizeBundles", "False").ToBoolean();
				case Env.production:
					return true;
				case Env.dev_testing:
					return true;
				case Env.local_test_sqlite:
					return GetAppSetting("OptimizeBundles", "True").ToBoolean();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static bool DisableMinification() {

			switch (GetEnv()) {
				case Env.local_sqlite:
					return GetAppSetting("DisableMinification", "False").ToBoolean();
				case Env.local_mysql:
					return GetAppSetting("DisableMinification", "False").ToBoolean();
				case Env.production:
					return false;
				case Env.dev_testing:
					return false;
				case Env.local_test_sqlite:
					return GetAppSetting("DisableMinification", "False").ToBoolean();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string GetTrelloKey() {
			return GetAppSetting("TrelloKey");
		}

		public static class Basecamp {
			/*public static string GetUrl()
			{
				return GetAppSetting("BasecampUrl");
			}*/

			public static string GetUserAgent() {
				switch (GetEnv()) {
					case Env.local_mysql:
						goto case Env.local_sqlite;
					case Env.local_sqlite:
						return GetAppSetting("BasecampTestApp");
					case Env.production:
						return GetAppSetting("BasecampApp");
					case Env.dev_testing:
						return GetAppSetting("BasecampApp");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			public static BCXAPI.Service GetService(OrganizationModel organization) {
				string key, secret;//, app;
				var redirect = BaseUrl(organization) + "Callback/Basecamp";
				switch (GetEnv()) {
					case Env.local_mysql:
						goto case Env.local_sqlite;
					case Env.local_sqlite: {
							key = GetAppSetting("BasecampTestKey");
							secret = GetAppSetting("BasecampTestSecret");
							break;
						}
					case Env.production: {
							key = GetAppSetting("BasecampKey");
							secret = GetAppSetting("BasecampSecret");
							break;
						}
					case Env.dev_testing: {
							key = GetAppSetting("BasecampKey");
							secret = GetAppSetting("BasecampSecret");
							break;
						}
					default:
						throw new ArgumentOutOfRangeException();
				}
				return new BCXAPI.Service(key, secret, redirect, GetUserAgent());
			}
		}

		public static bool SendEmails() {
			switch (GetEnv()) {
				case Env.local_mysql:
					return GetAppSetting("SendEmail_Debug", "false").ToBooleanJS();
				case Env.local_sqlite:
					return GetAppSetting("SendEmail_Debug", "false").ToBooleanJS();
				case Env.production:
					return true;
				case Env.local_test_sqlite:
					return false;
				case Env.dev_testing:
					return true;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string PaymentSpring_PublicKey(bool forceUseTest = false) {
			if (forceUseTest) {
				return GetAppSetting("PaymentSpring_PublicKey_Test");
			}

			switch (GetEnv()) {
				case Env.local_test_sqlite:
					return GetAppSetting("PaymentSpring_PublicKey_Test");
				case Env.local_mysql:
					return GetAppSetting("PaymentSpring_PublicKey_Test");
				case Env.local_sqlite:
					return GetAppSetting("PaymentSpring_PublicKey_Test");
				case Env.dev_testing:
					return GetAppSetting("PaymentSpring_PublicKey_Test");
				case Env.production:
					return GetAppSetting("PaymentSpring_PublicKey");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		[Obsolete("Be careful with private keys")]
		public static string PaymentSpring_PrivateKey(bool forceUseTest = false) {
			if (forceUseTest) {
				return GetAppSetting("PaymentSpring_PrivateKey_Test");
			}

			switch (GetEnv()) {
				case Env.local_test_sqlite:
					return GetAppSetting("PaymentSpring_PrivateKey_Test");
				case Env.local_mysql:
					return GetAppSetting("PaymentSpring_PrivateKey_Test");
				case Env.local_sqlite:
					return GetAppSetting("PaymentSpring_PrivateKey_Test");
				case Env.dev_testing:
					return GetAppSetting("PaymentSpring_PrivateKey_Test");
				case Env.production:
					return GetAppSetting("PaymentSpring_PrivateKey");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string VideoConferenceUrl(string resource = null) {
			var server = GetAppSetting("VideoConferenceServer").TrimEnd('/');
			if (resource != null) {
				server = server + "/" + resource.TrimStart('/');
			}
			return server;
			/*
			switch (GetEnv())
			{
				case Env.local_mysql:   return server;
				case Env.local_sqlite:	return GetAppSetting("VideoConferenceServer");
				case Env.production:	return GetAppSetting("VideoConferenceServer");
				default: throw new ArgumentOutOfRangeException();
			}*/
		}

		public static string GetMandrillGoogleAnalyticsDomain() {
			return GetAppSetting("Mandrill_GoogleAnalyticsDomain", null);
		}

		/*internal static string PaymentEmail()
		{
			throw new NotImplementedException();
		} */
		public static string GetAccessLogDir() {
			switch (GetEnv()) {
				case Env.local_mysql:
					return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
				case Env.local_sqlite:
					return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
				case Env.production:
					return @"C:\inetpub\logs\LogFiles\W3SVC1\";
				case Env.dev_testing:
					return @"C:\inetpub\logs\LogFiles\W3SVC1\";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string NotesUrl(string append = "") {
			var server = GetAppSetting("NotesServer", "https://note.dlptools.com");
			if (!string.IsNullOrWhiteSpace(append)) {
				server = server.TrimEnd('/') + "/" + append;
			}
			return server;
		}

		public static long GetTractionToolsOrgId() {
			return -1;//REPLACE_ME
		}

		public static long[] GetDisallowedOrgIds(ISession s) {
			return new[] { GetTractionToolsOrgId(), -2 /*EOSWW*/};
		}

		internal static string NoteApiKey() {
			return GetAppSetting("NotesServer_ApiKey");
		}

		public static class Office365 {
			public static string AppId() {
				return GetAppSetting("ida:AppId");
			}
			public static string Password() {
				return GetAppSetting("ida:AppPassword");
			}

			public static string RedirectUrl() {
				return BaseUrl(null);
			}
			public static string[] Scopes() {
				return GetAppSetting("ida:AppScopes").Replace(' ', ',').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			}
		}


		public class RedisConfig {
			public string Server { get; set; }
			public int Port { get; set; }
			public string Password { get; set; }
			public string ChannelName { get; set; }
		}

		public static RedisConfig RedisSignalR(string channel) {
			string server;
			switch (GetEnv()) {
				case Env.local_mysql:
					server = "127.0.0.1";
					break;
				case Env.local_sqlite:
					server = "127.0.0.1";
					break;
				case Env.local_test_sqlite:
					server = "127.0.0.1";
					break;
				case Env.production:
					server = GetAppSetting("RedisSignalR_Server", null);
					break;
				case Env.dev_testing:
					server = GetAppSetting("RedisSignalR_DevServer", null);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return new RedisConfig() {
				Server = server,
				ChannelName = channel,
				Password = GetAppSetting("RedisSignalR_Password", null),
				Port = GetAppSetting("RedisSignalR_Port", "6379").ToInt()
			};


		}

		

		public class TwilioData {
			public string Sid { get; set; }
			public string AuthToken { get; set; }
			public bool ShouldSendText { get; set; }
		}

		public static TwilioData Twilio() {
			return new TwilioData() {
				Sid = Config.GetAppSetting("TwilioSid"),
				AuthToken = Config.GetAppSetting("TwilioToken"),
				ShouldSendText = !Config.IsLocal()
			};
		}

		public class AsanaData {
			public string ClientId { get; set; }
			public string ClientSecret { get; set; }
		}
		public static AsanaData Asana() {
			return new AsanaData() {
				ClientId = Config.GetAppSetting("Asana_ClientId"),
				ClientSecret = Config.GetAppSetting("Asana_ClientSecret"),
			};
		}

		public static string GetSlackAuthToken() {
			//TT-bot's token
			return Config.GetAppSetting("SlackToken", "REPLACE_ME");
		}

		public static bool ReadonlyMode() {
			if (Config.GetAppSetting("ReadonlyMode", "false").ToLower() == "true") {
				return true;
			}
			return false;
		}

		public class SignalREndpointData {
			public string EndpointPattern { get; set; }
			public string EndpointCount { get; set; }
		}

		public static SignalREndpointData GetSignalrEndpoint() {
			return new SignalREndpointData {
				EndpointCount = Config.GetAppSetting("SignalrEndpointCount", "21"),
				EndpointPattern = Config.GetAppSetting("SignalrEndpointPattern", "https://s{0}.dlptools.com/signalr")
			};
		}


	}
}
