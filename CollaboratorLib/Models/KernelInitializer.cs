// KernelInitializer.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Lazy singleton wrapper around KernelFactory. All callers (WorkflowRunner,
    /// PromptTestRunner, Collaborator) share one Kernel instance built by one factory.
    /// </summary>
    public static class KernelInitializer
    {
        private static Kernel? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// The shared Kernel instance. Built on first access via KernelFactory.Build().
        /// </summary>
        public static Kernel Kernel
        {
            get
            {
                if (_instance is not null) return _instance;
                lock (_lock)
                {
                    if (_instance is not null) return _instance;
                    _instance = KernelFactory.Build();
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Initializes the shared Kernel with a loggerFactory if not yet built.
        /// Called by Collaborator.EnsureKernelInitialized, which has the loggerFactory.
        /// No-op if the Kernel is already initialized.
        /// </summary>
        internal static Kernel EnsureBuilt(ILoggerFactory? loggerFactory)
        {
            if (_instance is not null) return _instance;
            lock (_lock)
            {
                if (_instance is not null) return _instance;
                _instance = KernelFactory.Build(loggerFactory);
                return _instance;
            }
        }
    }
}
