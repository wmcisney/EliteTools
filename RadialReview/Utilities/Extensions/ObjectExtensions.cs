﻿using NHibernate;
using NHibernate.Proxy;
using RadialReview.Models.Interfaces;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace RadialReview {
	public static class ObjectExtensions {
		public static bool Delete(this IDeletable self, ISession s, DateTime? now = null) {
			return DeleteOrUndelete(self, s, true, now);
		}
		public static bool Undelete(this IDeletable self, ISession s, DateTime? now = null) {
			return DeleteOrUndelete(self, s, false, now);
		}

		public static bool DeleteOrUndelete(this IDeletable self, ISession s, bool delete, DateTime? now = null) {
			if (delete) {
				if (self.DeleteTime == null) {
					self.DeleteTime = now ?? DateTime.UtcNow;
					s.Update(self);
					return true;
				}
				return false;
			} else {
				if (self.DeleteTime != null) {
					self.DeleteTime = null;
					s.Update(self);
					return true;
				}
				return false;
			}
		}

		public static DateTime? TryParseDateTime(this string str) {
			DateTime o;
			if (DateTime.TryParse(str, out o)) {
				return o;
			}

			return null;
		}
		public static DateTime TryParseDateTime(this string str, DateTime deflt) {
			return TryParseDateTime(str) ?? deflt;
		}

		public static decimal TryParseDecimal(this string str, decimal deflt) {
			return str.TryParseDecimal() ?? deflt;
		}
		public static decimal? TryParseDecimal(this string str) {
			decimal o;
			str = str.NotNull(x => x.TrimEnd('%', '$').TrimStart('%', '$'));
			if (decimal.TryParse(str, out o)) {
				return o;
			}

			return null;
		}
		public static decimal? TryParseDecimal(this string str, NumberStyles styles, IFormatProvider provider) {
			decimal o;
			if (decimal.TryParse(str, styles, provider, out o)) {
				return o;
			}

			return null;
		}
		public static long[] TryParseLongList(this string str) {
			if (str == null) {
				return new long[0];
			}
			return str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.TryParseLong())
				.Where(x => x != null)
				.Select(x => x.Value)
				.ToArray();
		}

		public static bool? TryParseBool(this string str) {
			if (str == null) {
				return null;
			}

			if (str.ToLower() == "true" || str.ToLower() == "true,false") {
				return true;
			}

			if (str.ToLower() == "false") {
				return false;
			}

			return null;
		}

		public static long? TryParseLong(this string str) {
			long o;
			if (long.TryParse(str, out o)) {
				return o;
			}

			return null;
		}
		public static int? TryParseInt(this string str) {
			int o;
			if (int.TryParse(str, out o)) {
				return o;
			}

			return null;
		}

		public static R NotEOSW<R>(this long orgId, R whenNotEOSW, R whenEOSW) {
			if (orgId == 1795) {
				return whenEOSW;
			}
			return whenNotEOSW;
		}

		//NotNull
		public static R NotNull<T, R>(this T obj, Func<T, R> f) {
			var proxy = obj as INHibernateProxy;
			if (proxy == null || !proxy.HibernateLazyInitializer.IsUninitialized || (proxy.HibernateLazyInitializer.Session != null && proxy.HibernateLazyInitializer.Session.IsOpen)) {
				if (obj != null) {
					try {
						return f(obj);
					} catch (NullReferenceException) {
						return default(R);
					} catch (ArgumentNullException) {
						return default(R);
					}
				} else {
					return default(R);
				}

				//return obj != null ? f(obj): default(R);
			}
			return default(R);
		}

		public static int ToInt(this Boolean b) {
			return b ? 1 : 0;
		}
		public static long ToLong(this Boolean b) {
			return b ? 1 : 0;
		}

		public static string ToPhoneNumber(this long s) {
			var input = s.ToString();
			var phone = "";
			if (input.Length == 11 && input.Substring(0, 1) == "1") {
				phone = input.Substring(0, 1) + "-" + input.Substring(1, 3) + "-" + input.Substring(4, 3) + "-" + input.Substring(7, 4);
			} else if (input.Length == 11 && input.Substring(0, 1) == "6") {
				phone = "+" + input.Substring(0, 2) + " " + input.Substring(2, 3) + " " + input.Substring(5, 3) + " " + input.Substring(8, 3) + " (AU only)";
			} else if (input.Length == 12 && input.Substring(0, 2) == "44") {
				phone = "+" + input.Substring(0, 2) + " " + input.Substring(2, 4) + " " + input.Substring(6, 6) + " (UK only)";
			} else if (input.Length == 10 && input.Substring(0, 3) == "204") {
				phone = "(" + input.Substring(0, 3) + ") " + input.Substring(3, 3) + "-" + input.Substring(6, 4) + " (Canada only)";
			} else if (input.Length == 12) {
				phone = "+" + input.Substring(0, 2) + " " + input.Substring(2, 4) + " " + input.Substring(6, 6);
			} else if (input.Length==1){
				phone = "(727) 555-1212";
			} else {
				phone = "(" + input.Substring(0, 3) + ") " + input.Substring(3, 3) + "-" + input.Substring(6, 4);
			}

			return phone;
		}
		public static long ToLong(this String s) {
			return long.Parse(s);
		}
		public static int ToInt(this String s) {
			return int.Parse(s);
		}
		public static double ToDouble(this String s) {
			return double.Parse(s);
		}
		public static decimal ToDecimal(this String s) {
			return decimal.Parse(s);
		}

		public static byte[] ToBytes(this String s) {
			return new UTF8Encoding().GetBytes(s);
		}

		public static bool ToBoolean(this String s) {
			return bool.Parse(s);
		}
		public static bool ToBooleanJS(this String s) {
			if (s == null) {
				return false;
			}

			var l = s.ToLower();
			return l.Contains("true") || l == "on";
		}

		public static DateTime ToDateTime(this String s, String format, double offset = 0.0) {
			var provider = CultureInfo.InvariantCulture;
			return DateTime.ParseExact(s, format, provider).AddHours(offset);
		}



		public static T Touch<T>(this T self) where T : IEnumerable {
			foreach (var o in self) {
				if (o is IEnumerable) {
					((IEnumerable)o).Touch();
				}
			}
			return self;
		}

	}
}

namespace RadialReview.AliveExtensions {
	public static class ObjectExtensions {
		public static bool Alive(this object obj) {
			if (obj is IDeletable) {
				return ((IDeletable)obj).DeleteTime == null;
			}

			return true;
		}
	}
}

namespace RadialReview.Nhibernate {
	public static class ObjectExtensions {
		public static T Unproxy<T>(this ISession s, T obj) {
			return (T)s.GetSessionImplementation().PersistenceContext.Unproxy(obj);
		}
	}
}

namespace RadialReview.Reflection {

	public static class ObjectExtensions {
		public static TRef Get<T, TRef>(this T obj, Expression<Func<T, TRef>> selector) {
			return selector.Compile()(obj);
		}
		public static void Set<T, TRef>(this T obj, Expression<Func<T, TRef>> selector, TRef value) {
			var prop = (PropertyInfo)((MemberExpression)selector.Body).Member;
			prop.SetValue(obj, value, null);
		}
	}
}