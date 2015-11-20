#If _MyType <> "Empty" Then

namespace My
{
	/// <summary>
	/// Module used to define the properties that are available in the My Namespace for Web projects.
	/// </summary>
	/// <remarks></remarks>
	[Microsoft.VisualBasic.HideModuleName()]
	class MyWebExtension
	{
		private ThreadSafeObjectProvider<global::Microsoft.VisualBasic.Devices.ServerComputer> s_Computer = new ThreadSafeObjectProvider<global::Microsoft.VisualBasic.Devices.ServerComputer>();
		private ThreadSafeObjectProvider<global::Microsoft.VisualBasic.ApplicationServices.WebUser> s_User = new ThreadSafeObjectProvider<global::Microsoft.VisualBasic.ApplicationServices.WebUser>();
		private ThreadSafeObjectProvider<global::Microsoft.VisualBasic.Logging.AspLog> s_Log = new ThreadSafeObjectProvider<global::Microsoft.VisualBasic.Logging.AspLog>();

		private ThreadSafeObjectProvider<MyApplication> s_Application = new ThreadSafeObjectProvider<MyApplication>();
		/// <summary>
		/// Returns information about the current application.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal MyApplication Application {
			get { return s_Application.GetInstance(); }
		}

		/// <summary>
		/// Returns information about the host computer.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal global::Microsoft.VisualBasic.Devices.ServerComputer Computer {
			get { return s_Computer.GetInstance(); }
		}
		/// <summary>
		/// Returns information for the current Web user.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal global::Microsoft.VisualBasic.ApplicationServices.WebUser User {
			get { return s_User.GetInstance(); }
		}
		/// <summary>
		/// Returns Request object.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[System.ComponentModel.Design.HelpKeyword("My.Request")]
		internal global::System.Web.HttpRequest Request {
			[System.Diagnostics.DebuggerHidden()]
			get {
				global::System.Web.HttpContext CurrentContext = global::System.Web.HttpContext.Current;
				if (CurrentContext != null) {
					return CurrentContext.Request;
				}
				return null;
			}
		}
		/// <summary>
		/// Returns Response object.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[System.ComponentModel.Design.HelpKeyword("My.Response")]
		internal global::System.Web.HttpResponse Response {
			[System.Diagnostics.DebuggerHidden()]
			get {
				global::System.Web.HttpContext CurrentContext = global::System.Web.HttpContext.Current;
				if (CurrentContext != null) {
					return CurrentContext.Response;
				}
				return null;
			}
		}
		/// <summary>
		/// Returns the Asp log object.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal global::Microsoft.VisualBasic.Logging.AspLog Log {
			[System.Diagnostics.DebuggerHidden()]
			get { return s_Log.GetInstance(); }
		}

		/// <summary>
		/// Provides access to WebServices added to this project.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[System.ComponentModel.Design.HelpKeyword("My.WebServices")]
		internal MyWebServices WebServices {
			[System.Diagnostics.DebuggerHidden()]
			get { return m_MyWebServicesObjectProvider.GetInstance(); }
		}

		[System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
		[Microsoft.VisualBasic.MyGroupCollection("System.Web.Services.Protocols.SoapHttpClientProtocol", "Create__Instance__", "Dispose__Instance__", "")]
		[System.Runtime.CompilerServices.CompilerGenerated()]
		internal sealed class MyWebServices
		{

			[System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), System.Diagnostics.DebuggerHidden()]
			public override bool Equals(object o)
			{
				return base.Equals(o);
			}
			[System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), System.Diagnostics.DebuggerHidden()]
			public override int GetHashCode()
			{
				return base.GetHashCode;
			}
			[System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), System.Diagnostics.DebuggerHidden()]
			internal global::System.Type GetType()
			{
				return typeof(MyWebServices);
			}
			[System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), System.Diagnostics.DebuggerHidden()]
			public override string ToString()
			{
				return base.ToString;
			}

			[System.Diagnostics.DebuggerHidden()]
			private static T Create__Instance__<T>(T instance) where T : new()
			{
				if (instance == null) {
					return new T();
				} else {
					return instance;
				}
			}

			[System.Diagnostics.DebuggerHidden()]
			private void Dispose__Instance__<T>(ref T instance)
			{
				instance = null;
			}

			[System.Diagnostics.DebuggerHidden()]
			[System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
			public MyWebServices()
			{
				base.New();
			}
		}

		[System.Runtime.CompilerServices.CompilerGenerated()]
		private readonly ThreadSafeObjectProvider<MyWebServices> m_MyWebServicesObjectProvider = new ThreadSafeObjectProvider<MyWebServices>();
	}

	[System.Runtime.CompilerServices.CompilerGenerated(), System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
	internal partial class MyApplication : global::Microsoft.VisualBasic.ApplicationServices.ApplicationBase
	{
	}

}

#End If
