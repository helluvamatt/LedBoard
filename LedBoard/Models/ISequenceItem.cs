using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedBoard.Models
{
	public interface ISequenceItem : INotifyPropertyChanged
	{
		/// <summary>
		/// Name to display for this transition
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Total length of the transition
		/// </summary>
		TimeSpan Length { get; set; }

		/// <summary>
		/// Get the default duration of the transition
		/// </summary>
		TimeSpan DefaultLength { get; }

		/// <summary>
		/// Type of the configuration object (if present)
		/// </summary>
		Type ConfigurationType { get; }

		/// <summary>
		/// Current configuration representation
		/// </summary>
		object CurrentConfiguration { get; }

		/// <summary>
		/// (Re)configure the transition
		/// </summary>
		/// <param name="newConfiguration"></param>
		void Configure(object newConfiguration);
	}
}
