using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.InvoicesProcess.Dtos;
using Shared.LoggingContext;
using Shared.Storage.DTOS;

namespace MyBudgetIA.Application.InvoicesProcess
{
    /// <inheritdoc cref="IInvoiceProcessService" />
    public class InvoiceProcessService(
        IBlobStorageService blobStorageService,
        ILogger<InvoiceProcessService> logger) : IInvoiceProcessService
    {
        /// <inheritdoc />
        public async Task ProcessAsync(
            InvoiceProcessingRequest request,
            InvoiceLoggingContext context,
            CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);

            // log stat

            // test params ?

            // get blob content
            // in try catch
            //var blob = await blobStorageService.DownloadBlobAsync(request.BlobName, cancellationToken);

            //log 

            // send to ia
        }
    }
}
