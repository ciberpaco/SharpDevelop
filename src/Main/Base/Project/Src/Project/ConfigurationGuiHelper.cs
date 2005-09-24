// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.SharpDevelop.Project
{
	/// <summary>
	/// Class that helps connecting configuration GUI controls to MsBuild properties.
	/// </summary>
	public class ConfigurationGuiHelper : ICanBeDirty
	{
		MSBuildProject project;
		Dictionary<string, Control> controlDictionary;
		List<ConfigurationGuiBinding> bindings = new List<ConfigurationGuiBinding>();
		
		public ConfigurationGuiHelper(MSBuildProject project, Dictionary<string, Control> controlDictionary)
		{
			this.project = project;
			this.controlDictionary = controlDictionary;
			this.configuration = project.Configuration;
			this.platform = project.Platform;
		}
		
		public MSBuildProject Project {
			get {
				return project;
			}
		}
		
		internal Dictionary<string, Control> ControlDictionary {
			get {
				return controlDictionary;
			}
		}
		
		#region Manage bindings
		public T GetProperty<T>(string property, T defaultValue, out PropertyStorageLocations location)
		{
			return project.GetProperty(configuration, platform, property, defaultValue, out location);
		}
		
		public void SetProperty<T>(string property, T value, PropertyStorageLocations location)
		{
			project.SetProperty(configuration, platform, property, value, location);
		}
		
		/// <summary>
		/// Initializes the Property and Project properties on the binding and calls the load method on it.
		/// Registers the binding so that Save is called on it when Save is called on the ConfigurationGuiHelper.
		/// </summary>
		public void AddBinding(string property, ConfigurationGuiBinding binding)
		{
			binding.Property = property;
			binding.Helper = this;
			binding.Load();
			bindings.Add(binding);
		}
		
		public void Load()
		{
			foreach (ConfigurationGuiBinding binding in bindings) {
				binding.Load();
			}
			IsDirty = false;
		}
		
		public bool Save()
		{
			foreach (ConfigurationGuiBinding binding in bindings) {
				if (!binding.Save()) {
					return false;
				}
			}
			IsDirty = false;
			return true;
		}
		
		void ControlValueChanged(object sender, EventArgs e)
		{
			IsDirty = true;
		}
		
		bool dirty;
		
		public bool IsDirty {
			get {
				return dirty;
			}
			set {
				if (dirty != value) {
					dirty = value;
					if (DirtyChanged != null) {
						DirtyChanged(this, EventArgs.Empty);
					}
				}
			}
		}
		
		public event EventHandler DirtyChanged;
		
		string configuration;
		
		public string Configuration {
			get {
				return configuration;
			}
			set {
				configuration = value;
			}
		}
		
		string platform;

		public string Platform {
			get {
				return platform;
			}
			set {
				platform = value;
			}
		}
		
		#region Bind bool to CheckBox
		public ConfigurationGuiBinding BindBoolean(string control, string property, bool defaultValue)
		{
			return BindBoolean(controlDictionary[control], property, defaultValue);
		}
		
		public ConfigurationGuiBinding BindBoolean(Control control, string property, bool defaultValue)
		{
			CheckBox checkBox = control as CheckBox;
			if (checkBox != null) {
				CheckBoxBinding binding = new CheckBoxBinding(checkBox, defaultValue);
				AddBinding(property, binding);
				checkBox.CheckedChanged += ControlValueChanged;
				return binding;
			} else {
				throw new ApplicationException("Cannot bind " + control.GetType().Name + " to bool property.");
			}
		}
		
		class CheckBoxBinding : ConfigurationGuiBinding
		{
			CheckBox control;
			bool defaultValue;
			
			public CheckBoxBinding(CheckBox control, bool defaultValue)
			{
				this.control = control;
				this.defaultValue = defaultValue;
			}
			
			public override void Load()
			{
				control.Checked = Get(defaultValue);
			}
			
			public override bool Save()
			{
				string oldValue = Get("True");
				if (oldValue == "true" || oldValue == "false") {
					// keep value in lower case
					Set(control.Checked.ToString().ToLower());
				} else {
					Set(control.Checked.ToString());
				}
				return true;
			}
		}
		#endregion
		
		#region Bind string to TextBox or ComboBox
		public ConfigurationGuiBinding BindString(string control, string property)
		{
			return BindString(controlDictionary[control], property);
		}
		
		public ConfigurationGuiBinding BindString(Control control, string property)
		{
			if (control is TextBoxBase || control is ComboBox) {
				SimpleTextBinding binding = new SimpleTextBinding(control);
				AddBinding(property, binding);
				control.TextChanged += ControlValueChanged;
				if (control is ComboBox) {
					control.KeyDown += ComboBoxKeyDown;
				}
				return binding;
			} else {
				throw new ApplicationException("Cannot bind " + control.GetType().Name + " to string property.");
			}
		}
		
		void ComboBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == (Keys.Control | Keys.S)) {
				e.Handled = true;
				new ICSharpCode.SharpDevelop.Commands.SaveFile().Run();
			}
		}
		
		class SimpleTextBinding : ConfigurationGuiBinding
		{
			Control control;
			
			public SimpleTextBinding(Control control)
			{
				this.control = control;
			}
			
			public override void Load()
			{
				control.Text = Get("");
			}
			
			public override bool Save()
			{
				Set(control.Text);
				return true;
			}
		}
		#endregion
		
		#region Bind hex number to TextBox
		public ConfigurationGuiBinding BindHexadecimal(TextBoxBase textBox, string property, int defaultValue)
		{
			HexadecimalBinding binding = new HexadecimalBinding(textBox, defaultValue);
			AddBinding(property, binding);
			textBox.TextChanged += ControlValueChanged;
			return binding;
		}
		
		class HexadecimalBinding : ConfigurationGuiBinding
		{
			TextBoxBase textBox;
			int defaultValue;
			
			public HexadecimalBinding(TextBoxBase textBox, int defaultValue)
			{
				this.textBox = textBox;
				this.defaultValue = defaultValue;
			}
			
			public override void Load()
			{
				int val;
				if (!int.TryParse(Get(defaultValue.ToString(NumberFormatInfo.InvariantInfo)), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out val)) {
					val = defaultValue;
				}
				textBox.Text = "0x" + val.ToString("x", NumberFormatInfo.InvariantInfo);
			}
			
			public override bool Save()
			{
				string txt = textBox.Text.Trim();
				NumberStyles style = NumberStyles.Integer;
				if (txt.StartsWith("0x")) {
					txt = txt.Substring(2);
					style = NumberStyles.HexNumber;
				}
				int val;
				if (!int.TryParse(txt, style, NumberFormatInfo.InvariantInfo, out val)) {
					textBox.Focus();
					// TODO: Translate Please enter a valid number.
					MessageService.ShowMessage("Please enter a valid number.");
					return false;
				}
				Set(val.ToString(NumberFormatInfo.InvariantInfo));
				return true;
			}
		}
		#endregion
		
		#region Bind enum to ComboBox
		/// <summary>
		/// Bind enum to ComboBox
		/// </summary>
		public ConfigurationGuiBinding BindEnum<T>(string control, string property, params T[] values) where T : struct
		{
			return BindEnum(controlDictionary[control], property, values);
		}
		
		/// <summary>
		/// Bind enum to ComboBox
		/// </summary>
		public ConfigurationGuiBinding BindEnum<T>(Control control, string property, params T[] values) where T : struct
		{
			Type type = typeof(T);
			if (values == null || values.Length == 0) {
				values = (T[])Enum.GetValues(type);
			}
			ComboBox comboBox = control as ComboBox;
			if (comboBox != null) {
				foreach (T element in values) {
					object[] attr = type.GetField(Enum.GetName(type, element)).GetCustomAttributes(typeof(DescriptionAttribute), false);
					string description;
					if (attr.Length > 0) {
						description = StringParser.Parse((attr[0] as DescriptionAttribute).Description);
					} else {
						description = Enum.GetName(type, element);
					}
					comboBox.Items.Add(description);
				}
				string[] valueNames = new string[values.Length];
				for (int i = 0; i < values.Length; i++)
					valueNames[i] = values[i].ToString();
				ComboBoxBinding binding = new ComboBoxBinding(comboBox, valueNames, valueNames[0]);
				AddBinding(property, binding);
				comboBox.SelectedIndexChanged += ControlValueChanged;
				comboBox.KeyDown += ComboBoxKeyDown;
				return binding;
			} else {
				throw new ApplicationException("Cannot bind " + control.GetType().Name + " to enum property.");
			}
		}
		
		/// <summary>
		/// Bind list of strings to ComboBox.
		/// entries: value -> Description
		/// </summary>
		public void BindStringEnum(string control, string property, string defaultValue, params KeyValuePair<string, string>[] entries)
		{
			BindStringEnum(controlDictionary[control], property, defaultValue, entries);
		}
		
		/// <summary>
		/// Bind list of strings to ComboBox.
		/// entries: value -> Description
		/// </summary>
		public void BindStringEnum(Control control, string property, string defaultValue, params KeyValuePair<string, string>[] entries)
		{
			ComboBox comboBox = control as ComboBox;
			if (comboBox != null) {
				string[] valueNames = new string[entries.Length];
				for (int i = 0; i < entries.Length; i++) {
					valueNames[i] = entries[i].Key;
					comboBox.Items.Add(StringParser.Parse(entries[i].Value));
				}
				AddBinding(property, new ComboBoxBinding(comboBox, valueNames, defaultValue));
				comboBox.SelectedIndexChanged += ControlValueChanged;
				comboBox.KeyDown += ComboBoxKeyDown;
			} else {
				throw new ApplicationException("Cannot bind " + control.GetType().Name + " to enum property.");
			}
		}
		
		class ComboBoxBinding : ConfigurationGuiBinding
		{
			ComboBox control;
			string[] values;
			string defaultValue;
			
			public ComboBoxBinding(ComboBox control, string[] values, string defaultValue)
			{
				this.control = control;
				this.values = values;
				this.defaultValue = defaultValue;
			}
			
			public override void Load()
			{
				string val = Get(defaultValue);
				int i;
				for (i = 0; i < values.Length; i++) {
					if (val.Equals(values[i], StringComparison.OrdinalIgnoreCase))
						break;
				}
				if (i == values.Length) i = 0;
				control.SelectedIndex = i;
			}
			
			public override bool Save()
			{
				Set(values[control.SelectedIndex]);
				return true;
			}
		}
		#endregion
		
		#region Bind enum to RadioButtons
		/// <summary>
		/// Bind enum to RadioButtons
		/// </summary>
		public ConfigurationGuiBinding BindRadioEnum<T>(string property, params KeyValuePair<T, RadioButton>[] values) where T : struct
		{
			RadioEnumBinding<T> binding = new RadioEnumBinding<T>(values);
			AddBinding(property, binding);
			foreach (KeyValuePair<T, RadioButton> pair in values) {
				pair.Value.CheckedChanged += ControlValueChanged;
			}
			return binding;
		}
		
		class RadioEnumBinding<T> : ConfigurationGuiBinding where T : struct
		{
			KeyValuePair<T, RadioButton>[] values;
			
			internal RadioEnumBinding(KeyValuePair<T, RadioButton>[] values)
			{
				this.values = values;
			}
			
			public override void Load()
			{
				T val = Get(values[0].Key);
				int i;
				for (i = 0; i < values.Length; i++) {
					if (val.Equals(values[i].Key))
						break;
				}
				if (i == values.Length) i = 0;
				values[i].Value.Checked = true;
			}
			
			public override bool Save()
			{
				foreach (KeyValuePair<T, RadioButton> pair in values) {
					if (pair.Value.Checked) {
						Set(pair.Key);
						break;
					}
				}
				return true;
			}
		}
		#endregion
		#endregion
		
		#region ConfigurationSelector
		/// <summary>
		/// Gets the height of the configuration selector in pixel.
		/// </summary>
		public const int ConfigurationSelectorHeight = 30;
		
		public Control CreateConfigurationSelector()
		{
			return new ConfigurationSelector(this);
		}
		
		public void AddConfigurationSelector(Control parent)
		{
			foreach (Control ctl in parent.Controls) {
				ctl.Top += ConfigurationSelectorHeight;
			}
			Control sel = CreateConfigurationSelector();
			sel.Width = parent.ClientSize.Width;
			parent.Controls.Add(sel);
			parent.Controls.SetChildIndex(sel, 0);
			sel.Anchor |= AnchorStyles.Right;
		}
		
		sealed class ConfigurationSelector : Panel
		{
			ConfigurationGuiHelper helper;
			Label    configurationLabel = new Label();
			ComboBox configurationComboBox = new ComboBox();
			Label    platformLabel = new Label();
			ComboBox platformComboBox = new ComboBox();
			Control  line = new Control();
			
			public ConfigurationSelector(ConfigurationGuiHelper helper)
			{
				const int marginTop  = 4;
				const int marginLeft = 4;
				this.helper = helper;
				this.Height = ConfigurationSelectorHeight;
				configurationLabel.Text      = StringParser.Parse("${res:Dialog.ProjectOptions.Configuration}:");
				configurationLabel.TextAlign = ContentAlignment.MiddleRight;
				configurationLabel.Location  = new Point(marginLeft, marginTop);
				configurationLabel.Width     = 80;
				configurationComboBox.Location      = new Point(4 + configurationLabel.Right, marginTop);
				configurationComboBox.Width         = 120;
				configurationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
				platformLabel.Text      = StringParser.Parse("${res:Dialog.ProjectOptions.Platform}:");
				platformLabel.TextAlign = ContentAlignment.MiddleRight;
				platformLabel.Location  = new Point(4 + configurationComboBox.Right, marginTop);
				platformLabel.Width     = 68;
				platformComboBox.Location      = new Point(4 + platformLabel.Right, marginTop);
				platformComboBox.Width         = 120;
				platformComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
				line.Bounds    = new Rectangle(marginLeft, ConfigurationSelectorHeight - 2, Width - marginLeft * 2, ConfigurationSelectorHeight - 2);
				line.BackColor = SystemColors.ControlDark;
				this.Controls.AddRange(new Control[] { configurationLabel, configurationComboBox, platformLabel, platformComboBox, line });
				line.Anchor |= AnchorStyles.Right;
				FillBoxes();
				configurationComboBox.SelectedIndexChanged += ConfigurationChanged;
				platformComboBox.SelectedIndexChanged      += ConfigurationChanged;
			}
			
			void FillBoxes()
			{
				configurationComboBox.Items.Clear();
				configurationComboBox.Items.AddRange(helper.Project.GetConfigurationNames());
				platformComboBox.Items.Clear();
				platformComboBox.Items.AddRange(helper.Project.GetPlatformNames());
				ResetIndex();
			}
			
			bool resettingIndex;
			
			void ResetIndex()
			{
				resettingIndex = true;
				configurationComboBox.SelectedIndex = configurationComboBox.Items.IndexOf(helper.Configuration);
				platformComboBox.SelectedIndex      = platformComboBox.Items.IndexOf(helper.Platform);
				resettingIndex = false;
			}
			
			void ConfigurationChanged(object sender, EventArgs e)
			{
				if (resettingIndex) return;
				if (helper.IsDirty) {
					if (!MessageService.AskQuestion("${res:Dialog.ProjectOptions.ContinueSwitchConfiguration}")) {
						ResetIndex();
						return;
					}
					if (!helper.Save()) {
						ResetIndex();
						return;
					}
				}
				helper.Configuration = (string)configurationComboBox.SelectedItem;
				helper.Platform      = (string)platformComboBox.SelectedItem;
				helper.Load();
			}
		}
		#endregion
	}
}
