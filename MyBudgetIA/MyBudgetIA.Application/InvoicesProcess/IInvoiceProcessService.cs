using MyBudgetIA.Application.InvoicesProcess.Dtos;
using Shared.LoggingContext;

namespace MyBudgetIA.Application.InvoicesProcess
{
    /// <summary>
    /// Defines the contract for processing invoices within the application.
    /// Orchestrates the workflow of handling invoice-related operations.
    /// </summary>
    public interface IInvoiceProcessService
    {
        /// <summary>
        /// Processes a queue message asynchronously using the provided payload.
        /// </summary>
        /// <param name="request">The request object containing the necessary information for processing the invoice.</param>   
        /// <param name="context">The logging context for the invoice processing operation, used for tracking and logging purposes.</param>"
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation should be canceled if this token is triggered.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ProcessAsync(InvoiceProcessingRequest request, InvoiceLoggingContext context, CancellationToken cancellationToken);
    }
}
