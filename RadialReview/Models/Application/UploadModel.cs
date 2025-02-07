﻿using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using System;

namespace RadialReview.Models.Application {
	public class UploadModel {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual UploadType UploadType { get; set; }
		public virtual string OriginalName { get; set; }
		public virtual string Identifier { get; set; }
		public virtual string MimeType { get; set; }
		public virtual ForModel ForModel { get; set; }
		public virtual byte[] _Data { get; set; }
		//public virtual Stream _Stream { get; set; }

		public UploadModel() {
			CreateTime = DateTime.UtcNow;
			Identifier = Guid.NewGuid().ToString();
		}


		public virtual string GetPath(bool includeBucket = false) {
			var path = UploadType + "/" + Identifier;
			if (includeBucket) {
				return "elitetools/" + path;
			}

			return path;
		}

		public class Map : ClassMap<UploadModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreatedBy);
				Map(x => x.OrganizationId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.UploadType);
				Map(x => x.OriginalName);
				Map(x => x.Identifier).Column("awsid");
				Map(x => x.MimeType);
				Component(x => x.ForModel).ColumnPrefix("ForModel_");
			}
		}

	}
}