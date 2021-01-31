using System;

namespace RD_AAOW
	{
	/// <summary>
	/// Интерфейс описывает общий функционал для оповещений всех типов
	/// </summary>
	public interface INotification: IDisposable, IComparable, IEquatable<object>
		{
		/// <summary>
		/// Возвращает признак начала искомого участка страницы
		/// </summary>
		string Beginning
			{
			get;
			}

		/// <summary>
		/// Возвращает последний успешно полученный участок текста
		/// </summary>
		string CurrentText
			{
			get;
			}

		/// <summary>
		/// Возвращает признак окончания искомого участка страницы
		/// </summary>
		string Ending
			{
			get;
			}

		/// <summary>
		/// Возвращает или устанавливает флаг, указывающий на активность оповещения
		/// </summary>
		bool IsEnabled
			{
			get;
			set;
			}

		/// <summary>
		/// Возвращает флаг, указывающий на успешную инициализацию оповещения
		/// </summary>
		bool IsInited
			{
			get;
			}

		/// <summary>
		/// Возвращает ссылку на отслеживаемый ресурс
		/// </summary>
		string Link
			{
			get;
			}

		/// <summary>
		/// Возвращает название оповещения
		/// </summary>
		string Name
			{
			get;
			}

		/// <summary>
		/// Возвращает порядковый номер (от начала, начиная с единицы) искомого вхождения целевого ограничителя
		/// </summary>
		uint OccurrenceNumber
			{
			get;
			}

		/// <summary>
		/// Возвращает множитель частоты обновления оповещения
		/// </summary>
		uint UpdateFrequency
			{
			get;
			}

		/// <summary>
		/// Метод сбрасывает счётчик отображения оповещения
		/// </summary>
		/// <param name="ResetText">Флаг указывает на полный сброс состояния</param>
		void ResetTimer (bool ResetText);

		/// <summary>
		/// Метод обновляет состояние оповещения
		/// </summary>
		/// <returns>Возвращает true в случае успешного получения нового оповещения</returns>
		bool Update ();

#if ANDROID
		/// <summary>
		/// Возвращает или задаёт последний успешно полученный участок текста 
		/// (для сохранения состояния в ОС Android)
		/// </summary>
		string UnprocessedCurrentText
			{
			get;
			set;
			}
#endif
		}
	}