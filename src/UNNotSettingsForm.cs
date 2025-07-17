using System;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек отдельного оповещения
	/// </summary>
	public partial class UNNotSettingsForm: Form
		{
		// Переменные и константы
		private Notification oldNotificationItem;
		private Notification newNotificationItem;

		/// <summary>
		/// Возвращает доступное количество частот обновления уведомлений
		/// </summary>
		public const uint AvailableFrequencies = 24;

		/// <summary>
		/// Возвращает true, если изменения в окне настроек были применены
		/// </summary>
		public bool ChangesApplied
			{
			get
				{
				return (newNotificationItem != null);
				}
			}

		/// <summary>
		/// Возвращает новое настроенное уведомление или null, если настройка
		/// не была выполнена
		/// </summary>
		public Notification NewNotificationItem
			{
			get
				{
				return newNotificationItem;
				}
			}

		/// <summary>
		/// Конструктор. Инициализирует форму настройки оповещения
		/// </summary>
		/// <param name="NotItem">Оповещение для настройки</param>
		/// <param name="UpdatingFrequencyStep">Шаг изменения частоты обновления</param>
		public UNNotSettingsForm (Notification NotItem, uint UpdatingFrequencyStep)
			{
			// Инициализация
			InitializeComponent ();
			oldNotificationItem = NotItem;

			this.Text = ProgramDescription.AssemblyVisibleName;
			this.CancelButton = BClose;
			this.AcceptButton = BApply;

			RDGenerics.LoadWindowDimensions (this);

			// Настройка контролов
			for (uint i = 1; i <= AvailableFrequencies; i++)
				FrequencyCombo.Items.Add ((i * UpdatingFrequencyStep).ToString ());
			FrequencyCombo.SelectedIndex = 2;

			OccurrenceField.Minimum = 1;
			OccurrenceField.Maximum = Notification.MaxOccurrenceNumber;

			NameText.MaxLength = BeginningText.MaxLength = EndingText.MaxLength = Notification.MaxBeginningEndingLength;

			ComparatorValue.MouseWheel += ComparatorValue_MouseWheel;

			// Локализация
			RDLocale.SetControlsText (this);
			BApply.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Apply);
			BClose.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel);

			char[] ctSplitter = [ '\n' ];
			ComparatorType.Items.AddRange (RDLocale.GetText ("ComparatorTypes").Split (ctSplitter));

			// Загрузка параметров
			NameText.Text = oldNotificationItem.Name;
			LinkText.Text = oldNotificationItem.Link;
			BeginningText.Text = oldNotificationItem.Beginning;
			EndingText.Text = oldNotificationItem.Ending;
			FrequencyCombo.SelectedIndex = (int)oldNotificationItem.UpdateFrequency - 1;
			EnabledCheck.Checked = oldNotificationItem.IsEnabled;
			OccurrenceField.Value = oldNotificationItem.OccurrenceNumber;

			ComparatorFlag.Checked = (oldNotificationItem.ComparisonType != NotComparatorTypes.Disabled);
			ComparatorValue.Text = oldNotificationItem.ComparisonString;
			MisfitsFlag.Checked = oldNotificationItem.IgnoreComparisonMisfits;
			CheckAvailability.Checked = oldNotificationItem.NotifyIfSourceIsUnavailable;

			if (ComparatorFlag.Checked)
				ComparatorType.SelectedIndex = (int)oldNotificationItem.ComparisonType;
			else
				ComparatorType.SelectedIndex = 0;

			// Запуск
			this.ShowDialog ();
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Закрытие окна просмотра
		private void BApply_Click (object sender, EventArgs e)
			{
			// Инициализация оповещения
			NotConfiguration cfg;
			cfg.NotificationName = NameText.Text;
			cfg.SourceLink = LinkText.Text;
			cfg.WatchAreaBeginningSign = BeginningText.Text;
			cfg.WatchAreaEndingSign = EndingText.Text;
			cfg.UpdatingFrequency = (uint)(FrequencyCombo.SelectedIndex + 1);
			cfg.OccurrenceNumber = (uint)OccurrenceField.Value;
			cfg.ComparisonType = ComparatorFlag.Checked ? (NotComparatorTypes)ComparatorType.SelectedIndex :
				NotComparatorTypes.Disabled;
			cfg.ComparisonString = ComparatorValue.Text;
			cfg.IgnoreComparisonMisfits = MisfitsFlag.Checked;
			cfg.NotifyWhenUnavailable = CheckAvailability.Checked;

			Notification ni = new Notification (cfg);
			if (!ni.IsInited)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"NotEnoughDataMessage", 2000);
				return;
				}

			// Успешно
			newNotificationItem = ni;
			newNotificationItem.IsEnabled = EnabledCheck.Checked;
			this.Close ();
			}

		private void UNNotSettingsForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			RDGenerics.SaveWindowDimensions (this);
			}

		// Подсказка по полю Occurence
		private void OccurrenceField_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.OccurenceTip);
			}

		// Изменение состояния функции
		private void ComparatorFlag_CheckedChanged (object sender, EventArgs e)
			{
			if (ComparatorFlag.Checked)
				ProgramDescription.ShowTip (NSTipTypes.Threshold);

			ComparatorType.Enabled = ComparatorValue.Enabled = MisfitsFlag.Enabled = ComparatorFlag.Checked;
			}

		// Изменение значения компаратора
		private void ComparatorValue_KeyDown (object sender, KeyEventArgs e)
			{
			switch (e.KeyCode)
				{
				case Keys.Up:
				case Keys.Down:
					UpdateComparatorValue (e.KeyCode == Keys.Up);
					break;
				}
			}

		private void ComparatorValue_MouseWheel (object sender, MouseEventArgs e)
			{
			if (e.Delta > 0)
				UpdateComparatorValue (true);
			else if (e.Delta < 0)
				UpdateComparatorValue (false);
			}

		private void UpdateComparatorValue (bool Increase)
			{
			double v = 0.0;
			try
				{
				v = double.Parse (ComparatorValue.Text.Replace (',', '.'),
					RDLocale.GetCulture (RDLanguages.en_us));
				}
			catch { }

			if (Increase)
				v += 1.0;
			else
				v -= 1.0;

			ComparatorValue.Text = v.ToString (RDLocale.GetCulture (RDLanguages.en_us));
			}
		}
	}
