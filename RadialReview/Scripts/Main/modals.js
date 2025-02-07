﻿/*
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  obj ={																																			///
///	  title:,																																		///
///	  icon : <success,warning,danger,info,primary,default> or {icon:"css icon name",title:"Title Text!",color:"Hex-Color"}							///
///	  fields: [{																																	///
///		  name: (optional)																															///
///		  text: (optional)																															///
///		  type: <text,textarea,checkbox,radio,span,div,header,h1,h2,h3,h4,h5,h6,number,date,datetime,time,file,yesno,label,select,file>(optional)	///
///				   (if type=radio or select) options:[{text,value},...]																				///
///		  value: (optional)																															///
///		  placeholder: (optional)																													///
///		  classes: (optional)																														///
///	  },...],																																		///
///	  contents: jquery object (optional, overrides fields)																							///
///	  pushUrl:"",																																	///
///	  success:function(formData,contentType),																										///
///	  complete:function,																															///
///	  cancel:function,																																///
///	  reformat: function,																															///
///	  validation: function(data),																													///
///	  noCancel: bool																																///
///  }																																				///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
*/

/**
 * @typedef {Object} modalIconSettings
 * @property {string} [icon] - Icon Name. Either success,warning,danger,info,primary,default
 * @property {string} [title] - Modal heading
 * @property {string} [color] - Icon color. Hex
 */

/**
 * 
 * @typedef {Object} modalFieldSettings
 * @property {string} [name] - Field Name
 * @property {string} [text] - Label Text
 * @property {string} [type] - Input Type. Either text, textarea, checkbox, radio, span, div, header, h1, h2, h3, h4, h5, h6, number, date, datetime, time, file, yesno, label, select, file
 * @property {modalMultiInputOptions[]} [options] - Radio and Select options
 * @property {string} [value] - Value
 * @property {string} [placeholder] - Input placeholder text
 * @property {string} [classes] - classes to apply
 */

/**
 * @callback modalSuccessFunction
 * @property {Object} formData - The submission data
 * @property {string} contentType - Content type
 */

/**
 * 
 * @typedef {Object} modalMultiInputOptions
 * @property {string} text
 * @property {string} value
 */

/**
 * 
 * @typedef {Object} modalSettings
 * @property {string} title - Modal title
 * @property {string|modalIconSettings} [icon] - Icon to display. Either success,warning,danger,info,primary,default
 * @property {modalFieldSettings[]} [fields] - Fields for the modal
 * @property {string} [contents] - Html contents. Overrides fields
 * @property {string} [pushUrl] - Post the data to this url.
 * @property {modalSuccessFunction} [success] - Fired on success
 * @property {function} [complete] - Fired on complete
 * @property {function} [cancel] - Fired on cancel
 * @property {function} [reformat] - Function to reformat the form data before submission
 * @property {function} [validation] - Validation function. Return an error string to display the error. Return undefined for success.
 * @property {boolean} [noCancel] - Disable the cancel button
 */

/**
 * 
 * @param {string|modalSettings} setting - Modal settings (legacy: modal title)
 * @param {any} [pullUrl]
 * @param {any} [pushUrl]
 * @param {any} [callback]
 * @param {any} [validation]
 * @param {any} [onSuccess]
 * @param {any} [onCancel]
 * @param {any} [contentType]
 */
function showModal(title, pullUrl, pushUrl, callback, validation, onSuccess, onCancel, contentType) {
	//debugger;
	$("#modal").modal("hide");
	$("#modal-icon").attr("class", "");
	$("#modal #class-container").attr("class", "");
	$("#modalCancel").removeClass("hidden");
	$("#modalOk").removeClass("hidden");
	$(".modal-footer").removeClass("hidden");

	if (typeof (title) === "object") {
		var obj = title;
		var push = pullUrl;
		var cback = pushUrl;
		return showModalObject(obj, push, cback);
	}

	$("#modalTitle").html(title);
	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');

	$.ajax({
		url: pullUrl,
		type: "GET",
		//Couldnt retrieve modal partial view
		error: function (jqxhr, status, error) {
			$('#modal').modal('hide');
			$("#modal").removeClass("loading");
			$("#modalForm").unbind('submit');
			if (status == "timeout")
				showAlert("The request has timed out. If the problem persists, please contact us.");
			else
				showAlert("Something went wrong. If the problem persists, please contact us.");
		},
		//Retrieved Partial Modal
		success: function (modal) {
			if (modal && modal.Object) {
				showModalObject(modal.Object, pushUrl, onSuccess, onCancel);
			} else {
				if (!modal) {
					$('#modal').modal('hide');
					$("#modal").removeClass("loading");
					showAlert("Something went wrong. If the problem persists, please contact us.");
					return;
				}
				_bindModal(modal, title, callback, validation, function (formData) {
					_submitModal(formData, pushUrl, onSuccess, null, false, contentType);
				});
			}
		}
	});
}
function showModalObject(obj, pushUrl, onSuccess, onCancel) {
	var runAfterAnimation = [];
	$("#modal").modal("hide");
	$("#modalCancel").toggleClass("hidden", obj.noCancel || false);
	$("#modalOk").toggleClass("hidden", obj.noOk || false);
	$(".modal-footer").toggleClass("hidden", obj.noFooter || false);
	if (typeof (pushUrl) === "undefined")
		pushUrl = obj["push"] || obj["pushUrl"];
	if (typeof (onSuccess) === "undefined")
		onSuccess = obj["success"];
	if (typeof (onSuccess) !== "undefined" && typeof (pushUrl) !== "undefined") {
		var oldSuccess = onSuccess;
		onSuccess = function (formData, contentType) { _submitModal(formData, pushUrl, oldSuccess, obj.complete, true, contentType); };
	}
	if (typeof (onSuccess) === "undefined" && typeof (pushUrl) !== "undefined")
		onSuccess = function (formData, contentType) { _submitModal(formData, pushUrl, null, obj.complete, true, contentType); };

	var onClose = obj.close;

	if (typeof (onCancel) === "undefined")
		onCancel = obj["cancel"];

	if (!obj.fields && obj.pullUrl && obj.title && pushUrl)
		return showModal(obj.title, obj.pullUrl, pushUrl, onSuccess, obj.validation, obj.success);

	if (typeof (obj.title) === "undefined") {
		obj.title = "";
		console.warn("No title supplied");
	}

	obj.modalClass = obj.modalClass || "";

	var reformat = obj.reformat;
	var recalculateModalHeight = false;

	var iconType = typeof (obj.icon);
	if (iconType !== "undefined") {

		obj.modalClass += " modal-icon";
		$("#modal-icon").attr("class", "modal-icon");
		if (iconType === "string") {
			obj.modalClass += " modal-icon-" + obj.icon;
			//obj.title = iconType.toLowerCase() + "!";
		} else if (iconType === "object") {
			var time = +new Date();
			var custom = "modal-icon-custom" + time;
			obj.modalClass += " " + custom;
			if (!obj.icon.icon)
				obj.modalClass += " modal-icon-info";

			var icon = (obj.icon.icon || ("icon-" + custom)).replace(".", "");
			var title = escapeString(obj.icon.title || "Hey!");
			var color = escapeString(obj.icon.color || "#5bc0de");
			$("#modal-icon").addClass(icon);
			icon = icon.replace(" ", ".");
			//debugger;

			try {
				document.styleSheets[0].insertRule("." + custom + " ." + icon + ":after{content: '" + title + "' !important;}", 0);
				document.styleSheets[0].insertRule("." + custom + " ." + icon + ":before{ background-color: " + color + ";}", 0);
				document.styleSheets[0].insertRule("." + custom + " #modalOk{ background-color: " + color + ";}", 0);
			} catch (e) {
				console.error(e);
			}

			runAfterAnimation.push(function () {
				try {
					//debugger;
					var modalHeaderHeight = 125;
					var titleDiv = $("<div class='modal-icon-title'>" + title + "</div>");
					$("." + custom + " .modal-content").append(titleDiv);
					modalHeaderHeight += $(titleDiv).height();
					modalHeaderHeight += $("#modalTitle").height();
					titleDiv.remove();
					document.styleSheets[0].insertRule("." + custom + ".modal-icon .modal-header{ height: " + modalHeaderHeight + "px !important;}", 0);
				} catch (e) {
					console.error(e);
				}
			});
		}

	}

	$("#modal #class-container").attr("class", obj.modalClass);

	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');


	if (typeof (obj.field) !== "undefined") {
		if (typeof (obj.fields) !== "undefined") {
			throw "A 'field' and a 'fields' property exists";
		} else {
			obj.fields = obj.field;
		}
	}

	if (typeof (obj.fields) === "object") {
		var allDeep = true;
		for (var f in obj.fields) {
			if (arrayHasOwnIndex(obj.fields, f)) {
				if (typeof (obj.fields[f]) !== "object") {
					allDeep = false;
					break;
				}
			}
		}
		if (!allDeep) {
			obj.fields = [obj.fields];
		}
	}


	contentType = null;


	var runAfter = [];
	if (!obj.contents) {
		var fields = obj.fields;
		var result = FormFields(fields, obj);
		builder = result.html;
		contentType = result.contentType;
		runAfter = result.runAfter;

	} else {
		builder = $(obj.contents);
	}

	_bindModal(builder, obj.title, undefined, obj.validation, onSuccess, onCancel, reformat, onClose, contentType);
	setTimeout(function () {
		//debugger;
		for (var i = 0; i < runAfter.length; i++) {
			runAfter[i]();
		}
	}, 1);
	setTimeout(function () {
		for (var i = 0; i < runAfterAnimation.length; i++) {
			runAfterAnimation[i]();
		}
	}, 250);
}

function FormFields(fields, options) {

	var runAfter = [];
	var allowed = ["text", "hidden", "textarea", "checkbox", "radio", "number", "date", "time", "datetime", "header", "span", "div", "h1", "h2", "h3", "h4", "h5", "h6", "file", "yesno", "label", "img", "select", "readonly", "subform"];
	var addLabel = ["text", "textarea", "checkbox", "radio", "number", "date", "time", "datetime", "file", "select", "readonly", "subform"];
	var tags = ["span", "h1", "h2", "h3", "h4", "h5", "h6", "label", "div"];
	var anyFields = ""

	var defaultLabelColumnClass = options.labelColumnClass || "col-sm-2";
	var defaultValueColumnClass = options.valueColumnClass || "col-sm-10";

	var genInput = function (type, name, eid, placeholder, value, others, classes, tag) {
		others = others || "";
		classes = classes || "form-control blend";
		if (type == "number")
			others += " step=\"any\"";
		if (typeof (tag) === "undefined")
			tag = "input";

		if (type == "checkbox" && ((typeof (value) === "string" && (value.toLowerCase() === 'true')) || (typeof (value) === "boolean" && value)))
			others += "checked";

		if (type == "datetime") {
			var newVal = parseJsonDate(value, true).toISOString().substring(0, 19);
			type = "datetime-local";
			if (newVal)
				value = newVal;
		}
		return '<' + tag + ' type="' + escapeString(type) + '" class="' + classes + '"' +
					  ' name="' + escapeString(name) + '" id="' + eid + '" ' +
					  placeholder + ' value="' + escapeString(value) + '" ' + others + '/>';
	}

	var genSelect = function (name, options, eid, classes, selectedValue) {
		if (options != null && options.length > 0) {
			var fieldName = name;
			input = $(genInput("", fieldName, eid, null, null, selected, classes, "select"));
			for (var oid in options) {
				if (arrayHasOwnIndex(options, oid)) {
					var option = options[oid];
					if (!option.value) {
						console.warn("option has no value " + fieldName + "," + oid);
					}
					var optionId = eid + "_" + oid;
					var selected = option.checked || option.value == selectedValue || false;
					if (selected)
						selected = "selected";
					var optionText = option.text || option.value;
					var option = $(genInput("", fieldName, optionId, null, option.value, selected, " ", "option"));
					option.text(optionText);
					$(input).append(option);
				}
			}
			//input = $(input).html();
			return $(input).wrapAll('<div>').parent().html();
			//debugger;
			//input += "</table></fieldset>";
		} else {
			console.warn("select field requires an 'options' array");
			return "";
		}
	}

	var fieldsTypeIsArray = Array.isArray(fields);//typeof (obj.fields);
	var builder = '<div class="form-horizontal modal-builder">';
	for (var f in fields) {
		if (arrayHasOwnIndex(fields, f)) {
			try {
				var field = fields[f];
				var name = field.name || f;
				var label = typeof (field.text) !== "undefined" || !fieldsTypeIsArray;
				var text = field.text || name;
				var originalValue = field.value;
				var value = field.value || "";
				var placeholder = field.placeholder;
				var type = (field.type || "text").toLowerCase();
				var classes = field.classes || "";
				var onchange = field.onchange;
				var eid = escapeString(name);

				var labelColumnClass = field.labelColumnClass || defaultLabelColumnClass;
				var valueColumnClass = field.valueColumnClass || defaultValueColumnClass;

				if (typeof (classes) === "string" && (classes.indexOf('\'') != -1 || classes.indexOf('\"') != -1))
					throw "Classes cannot contain a quote character.";
				
				if (type == "header")
					type = "h4";

				if (typeof (placeholder) !== "undefined")
					placeholder = "placeholder='" + escapeString(placeholder) + "'";
				else
					placeholder = "";
				var input = "";
				var inputIndex = allowed.indexOf(type);
				if (inputIndex == -1) {
					console.warn("Input type not allowed:" + type);
					continue;
				}

				//Fields from datatable
				if (field.edit === true || field.remove === true) {
					continue;
				}

				if (Object.prototype.toString.call(value) === '[object Date]' && (/*type == "datetime" ||*/ type == "date")) {
					value = value.toISOString().substring(0, 10);
				}

				if (type == "file")
					options.contentType = 'enctype="multipart/form-data"';

				if (tags.indexOf(type) != -1) {
					var txt = value || text;
					input = "<" + type + " name=" + escapeString(name) + '" id="' + eid + '" class="' + classes + '">' + txt + '</' + type + '>';
				} else if (type == "textarea") {
					//input = '<script>tinymce.init({selector: \'textarea\'});</script><textarea class="form-control blend verticalOnly ' + classes + '" rows=5 name="' + escapeString(name) + '" id="' + eid + '" ' + escapeString(placeholder) + '>' + value + '</textarea>';
					input = '<textarea class="form-control blend verticalOnly ' + classes + '" rows=5 name="' + escapeString(name) + '" id="' + eid + '" ' + escapeString(placeholder) + '>' + value + '</textarea>';
				} else if (type == "date" /*|| type=="datetime"*/) {
					var guid = generateGuid();
					console.log(guid);
					var curName = name;
					var curVal = originalValue;
					var localize = field.localize;
					input = '<div class="date-container date-' + guid + ' ' + classes + '" id="' + eid + '"></div>';
					var runAfterFunc = function () {
						var g = guid;
						var cv = curVal;
						var cn = curName;
						var eidd = eid;
						var l = localize;
						return function () {
							var dateGenFunc = generateDatepicker;
							if (l == true)
								dateGenFunc = generateDatepickerLocalize;
							dateGenFunc('.date-' + g, cv, cn, eidd);
						}
					}();
					runAfter.push(runAfterFunc);
				} else if (type == "yesno") {
					var selectedYes = (value == true) ? 'checked="checked"' : "";
					var selectedNo = (value == true) ? "" : 'checked="checked"';
					input = '<div class="form-group input-yesno ' + classes + '">' +
								'<label for="true" class="col-xs-4 control-label"> Yes </label>' +
								'<div class="col-xs-2">' + genInput("radio", name, eid, placeholder, "true", selectedYes) + '</div>' +
								'<label for="false" class="col-xs-1 control-label"> No </label>' +
								'<div class="col-xs-2">' + genInput("radio", name, eid, placeholder, "false", selectedNo) + '</div>' +
							'</div>';
				} else if (type == "img") {
					input = "<img src='" + field.src + "' class='" + classes + "'/>";
				} else if (type == "radio") {

					if (field.options != null && field.options.length > 0) {
						var fieldName = name;
						input = "<fieldset id='group_" + fieldName + "'><table>";
						for (var oid in field.options) {
							if (arrayHasOwnIndex(field.options, oid)) {
								var option = field.options[oid];
								if (!option.value) {
									console.warn("option has no value " + fieldName + "," + oid);
								}
								var radioId = eid + "_" + oid;
								var selected = false;
								if (typeof (option.checked) === "function")
									selected = option.checked(field);
								else
									selected = option.checked;

								if (selected)
									selected = "checked";
								var radio = genInput("radio", fieldName, radioId, null, option.value, selected, option.classes || " ");
								var optionText = option.text || option.value;
								input += '<tr class="form-group">' +
											'<td><label for="' + radioId + '" class="pull-right ' + (option.labelColumnClass || "") + ' control-label" style="padding-right:10px;">' + optionText + '</label></td>' +
											'<td><div class="' + (option.valueColumnClass || "") + '" style="padding-top: 5px;">' + radio + '</div></td>' +
										 '</tr>';
							}
						}
						input += "</table></fieldset>";
					} else {
						console.warn("radio field requires an 'options' array");
					}
				} else if (type == "select") {
					input = genSelect(name, field.options, eid, classes, value);
				} else if (type == "readonly") {
					tag = "span";
					input = "<div class='" + classes + "' style='padding-top:7px;'>" + value + "</div>";
				} else if (type == "subform") {
					var oldOnChange = onchange;
					var subforms = field.subforms;
					var o = options;
					if (!subforms) {
						console.warn("Did you forget the subforms property?");
					}
					var opts = [];
					for (var key in subforms) {
						if (subforms.hasOwnProperty(key)) {
							var s = subforms[key];
							opts.push({ text: key, value: key });
						}
					};
					var ii;
					if (opts.length <= 1) {
						if (!value && opts.length == 1) {
							value = opts[0].value;
						}
						ii = $(genInput("hidden", name, eid, null, value, null, classes)).wrapAll('<div>').parent();
						text = "";
					} else {
						ii = $(genSelect(name, opts, eid, classes, value)).wrapAll('<div>').parent();
					}
					ii.addClass("subform-container");
					ii.append("<div class='subform-contents' style='padding-top:15px;'></div>");
					input = ii.wrapAll('<div>').parent().html();

					var after = function () {
						onchange = function () {
							var val = $(this).val();
							var subfields = subforms[val];

							var subform = $(this).closest(".subform-container").find(".subform-contents");
							var result = FormFields(subfields, o);
							var contents = result.html;
							subform.html(contents);
							var rrunafter = result.runAfter;
							if (typeof (oldOnChange) === "function")
								oldOnChange();

							setTimeout(function () {
								rrunafter.map(function (r) {
									r();
								});
							}, 1);
						};
						var iinput = input;
						var iname = name;
						return function () {
							onchange.bind($("[name='" + iname + "']"))();
						}
					}();

					runAfter.push(after);
				} else {
					input = genInput(type, name, eid, placeholder, value, null, classes);
				}

				if (addLabel.indexOf(type) != -1 && label) {
					builder += '<div class="form-group"><label for="' + name + '" class="' + labelColumnClass + ' control-label">' + text + '</label><div class="' + valueColumnClass + '">' + input + '</div></div>';
				} else {
					builder += input;
				}

				if (onchange) {
					if (typeof (onchange) === "function") {
						var after = function () {
							var ocf = onchange;
							var mname = name;
							return function () {
								$("[name=" + mname).on("change", ocf);
							}
						}();
						runAfter.push(after);
					} else {
						console.warn("Unhandled onchange type:" + typeof (onchange) + " for " + eid);
					}
				}

			} catch (e) {
				console.error(e);
			}
		}
	}
	builder += "</div>";
	return {
		html: builder,
		runAfter: runAfter,
		contentType: contentType,
	};

}

function _bindModal(html, title, callback, validation, onSuccess, onCancel, reformat, onClose, contentType) {
	$('#modalBody').html("");
	delete window.ModalValidation;

	setTimeout(function () {

		var error = $(html).find(".error-page");
		if (error.length >= 1) {
			$('#modal').modal('hide');
			var msg = error.find(".error-message").text() || "An error occurred.";
			showAlert(msg);
			return;
		}

		$('#modalBody').append(html);
	}, 0);

	$("#modalTitle").html(title);
	$("#modal").removeClass("loading");
	//Reregister submit button
	$("#modalForm").unbind('submit');

	var onCloseArg = onClose;
	var onCancelArg = onCancel;
	var onSuccessArg = onSuccess;
	var contentTypeArg = contentType;
	var validationArg = validation;
	var reformatArg = reformat;
	var callbackArg = callback;


	$("#modalBody").closest(".after-load").addClass("expanding");
	setTimeout(function () {
		$("#modalBody").closest(".after-load").removeClass("expanding");
	}, 350);
	setTimeout(function () {
		var dur = 100;
		var isSelect = false;
		var firstFocusable = $("#modalBody").closest(".after-load").find(":focusable").first();
		if (firstFocusable.is("select") || firstFocusable.is(".select2-selection--single")) {
			dur = 300;
			console.info("[Modal] Focus is select");
			isSelect = true;
		}
		if (firstFocusable.is(".client-date")) {
			return;//no focus on date.
		}

		console.info("[Modal] FirstFocusable",firstFocusable);
		setTimeout(function () {
			if ($(firstFocusable).is(".select2-hidden-accessible")) {
				firstFocusable = $(firstFocusable).next(".select2").find("input");
			}

			firstFocusable.focus();
			if (!isSelect) {
				setTimeout(function () {
					try {
						firstFocusable[0].selectionStart = firstFocusable[0].selectionEnd = 10000;
					} catch (e) {
						console.info("[Modal] Couldn't select from focusable");
					}
				}, 1);
			}
		}, dur);
	}, 200);
	//$("#modalForm input:visible,#modalForm textarea:visible,#modalForm button:not(.close):visible").first().focus();

	$("#modalForm").submit(function (ev) {
		ev.preventDefault();

		var formData = $("#modalForm").serializeObject();
		$("#modalForm").find("input:checkbox").each(function () {
			formData[$(this).prop("name")] = $(this).is(":checked") ? "True" : "False";
		});
		$("#modalForm").find(".input-yesno").each(function () {
			var name = $(this).find("input").attr("name");
			var v = $(this).find("[name=" + name + "]:checked").val();
			formData[name] = v == "true" ? "True" : "False";
		});

		if (typeof (reformatArg) === "function") {
			var o = reformatArg(formData);
			if (typeof (o) !== "undefined" && o != null)
				formData = o;//Data was returned, otherwise formdata was manipulated
		}

		validationArg = validationArg || window.ModalValidation;

		if (validationArg) {
			var message = undefined;
			if (typeof (validationArg) === "string") {
				message = window[validationArg](formData);
				//message = eval(validationArg + '()');
			} else if (typeof (validationArg) === "function") {
				message = validationArg(formData);
			}
			if (message !== undefined && message != true) {
				if (message == false) {
					$("#modalMessage").html("Error");
				}
				else {
					$("#modalMessage").html(message);
				}
				$("#modalMessage").removeClass("hidden");
				return;
			}
		}
		$("#modal").modal("hide");
		$("#modal").removeClass("loading");
		//onSuccess(formData);

		if (onSuccessArg) {
			if (typeof onSuccessArg === "string") {
				window[onSuccessArg](formData, contentTypeArg);
				//eval(onSuccessArg + "(formData," + contentTypeArg + ")");
			} else if (typeof onSuccessArg === "function") {
				onSuccessArg(formData, contentTypeArg);
			}
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				window[onCloseArg]();
				//eval(onCloseArg + "()");
			} else if (typeof onCloseArg === "function") {
				onCloseArg();
			}
		}
	});

	$("#modal button[data-dismiss='modal']").unbind('click.radialModal');


	$("#modal button[data-dismiss='modal']").on("click.radialModal", function () {
		if (typeof onCancelArg === "string") {
			window[onCancelArg]();
			//	eval(onCancelArg + "()");
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (typeof onCancelArg === "string") {
			//eval(onCancelArg + "()");
			window[onCancelArg]();
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				//eval(onCloseArg + "()");
				window[onCloseArg]();
			} else if (typeof onCloseArg === "function") {
				onCloseArg();
			}
		}
	});

	$("#modal").removeClass("loading");
	$('#modal').modal('show');
	var count = 0;
	setTimeout(function () {
		if (callbackArg) {
			if (typeof (callbackArg) === "string") {
				//eval(callbackArg + '()');
				window[callbackArg]();
			} else if (typeof (callbackArg) === "function")
				callbackArg();
		} else {
			//$('#modal input:not([type=hidden]):not(.disable):first').focus();
		}
	}, 50);
}

function _submitModal(formData, pushUrl, onSuccess, onComplete, useJson, contentType) {
	///FORM DATA IS NOT USED
	///TODO use form data;
	var serialized
	//var serialized = $.param(formData);
	//var contentType = null;

	if (typeof (contentType) === "undefined")
		contentType = null;
	var processData = null;
	if (useJson && contentType == null) {
		serialized = JSON.stringify(formData);
		contentType = "application/json; charset=utf-8";
	} else if (contentType == 'enctype="multipart/form-data"') {
		console.warn("[Modal] Using FormData will not work on IE9");
		serialized = new FormData($('#modalForm')[0]);
		processData = false;
		contentType = false;
	} else {
		serialized = $("#modalForm").serialize();
		contentType = contentType || "application/x-www-form-urlencoded";
	}
	var onSuccessArg = onSuccess;
	var onCompleteArg = onComplete;

	$.ajax({
		url: pushUrl,
		type: "POST",
		contentType: contentType,
		data: serialized,// JSON.stringify(formData),
		processData: processData,
		success: function (data, status, jqxhr) {
			if (!data) {
				$("#modal").modal("hide");
				$("#modal").removeClass("loading");
				showAlert("Something went wrong. If the problem persists, please contact us.");
			} else {
				if (onSuccessArg) {
					if (typeof onSuccessArg === "string") {
						window[onSuccessArg](data, formData);
						//eval(onSuccessArg + "(data,formData)");
					} else if (typeof onSuccessArg === "function") {
						onSuccessArg(data, formData);
					}
				} else {
				}
			}
		},
		complete: function (dd) {
			if (dd) {
				var data = dd.responseJSON;
				if (data) {
					if (onCompleteArg) {
						if (typeof onCompleteArg === "string") {
							window[onCompleteArg](data, formData);
							//eval(onCompleteArg + "(data,formData)");
						} else if (typeof onCompleteArg === "function") {
							onCompleteArg(data, formData);
						}
					}
				}
			}
		},
		error: function (jqxhr, status, error) {
			if (error == "timeout") {
				showAlert("The request has timed out. If the problem persists, please contact us.");
			} else {
				showAlert("Something went wrong. If the problem persists, please contact us.");
			}
			$("#modal").modal("hide");
			$("#modal").removeClass("loading");
		}
	});
}

/*
if callback returns text or bool, there is an error
*/
function showTextAreaModal(title, callback, defaultText) {
	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	if (typeof defaultText === "undefined")
		defaultText = "";

	$('#modalBody').html("<div class='error' style='color:red;font-weight:bold'></div><textarea class='form-control verticalOnly' rows=5>" + defaultText + "</textarea>");
	$("#modalTitle").html(title);
	$("#modalForm").unbind('submit');
	$("#modal").modal("show");

	$("#modalForm").submit(function (e) {
		e.preventDefault();
		var result = callback($('#modalBody').find("textarea").val());
		if (result) {
			if (typeof result !== "string") {
				result = "An error has occurred. Please check your input.";
			}
			$("#modalBody .error").html(result);
		} else {
			$("#modal").modal("hide");
		}
	});
}
