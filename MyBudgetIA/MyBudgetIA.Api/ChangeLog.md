---
- (09/02/2026):
  - == Blob Storage Integration ==
  - Added **BlobStorageService**" with method `UploadFileAsync`:
    - `UploadFileAsync` returns now an `PhotoUploadResult` to be more precise about the results of the batch upload
    - Batch upload support: `UploadFilesAsync` method added to handle multiple file uploads in a single operation, returning a list of `PhotoUploadResult` instances.
      - I choose the batch approach to avoid throwing exceptions during upload but instead allow the caller to handle individual file upload results,
      returning to user information about success/fail + reasons for each photo, without blocking the process.
      - The possible exceptions are catched and included in the `PhotoUploadResult` for each file, allowing the caller to handle them gracefully.
      - It will be usefull for the QueueStorage step, where a message will be created for each successfully uploaded photo.
  - Updated **PhotosController** :
    - updated endpoint `POST /api/photos/upload`:
      - return `PhotoUploadResult`
      - Convert ASP.NET Core IFormFile instances into application abstractions (`IFileUploadRequest`) via `FormFileAdapter`.
      
- (10/02/2026):
  - == Docker ==
    - Created a `Dockerfile` for the API project, enabling containerization of the application for consistent deployment across different environments.
    - Added a `docker-compose.yml` file to orchestrate multi-container applications, facilitating the setup of dependent services like databases or storage emulators alongside the API.
  - Changed **PhotoService** method `UploadPhotoAsync`:
    - instead of throwing a validation error, it now returns a `PhotoUploadResult` indicating the success or failure of the upload. Still have to work on the errors messages
      sent to the user.
    
- (12/02/2026):
  - == DevOps Pipeline ==
    - Created a `azure-pipelines.yml` file to define the CI/CD pipeline for the project, automating the build, test, and deployment processes.

- (14/02/2026):
  - Updated **BlobStorageService**" with method `DownloadBlobAsync`.
  - Added REST API endpoint `GetPhotoBlob{blobName}` in **PhotosController** to allow downloading photos from blob storage.

- (16/02/2026):
  - Updated **BlobStorageService**" with method `GetBlobsInfoAsync`.
  - Added REST API endpoint `GetUploadedPhotosInfos` in **PhotosController** to list all photos from blob storage.

- (22/02/2026):
  - == Queue Storage Integration ==
  - Added **QueueStorageService**" with method `EnqueueAsync`
  - Completed and modified `UploadPhotoAsync`:
    - Queue a message in Azure Queue Storage for each successfully uploaded blob.
    - For each photo, we return data about the upload result and the enqueue message, provinding information about the success or failure of both operations.
    - Moved the behavior of final total/partial success/failure response from the controller to the service.
    - Changed `UploadPhotoResult` validation error messages handling to provide more detailed information about the reasons for failure.
    - Moved stream validation to his own technical service to separate concerns and improve testability.
  - Changed `AzureStorageErrorMapper` into two mappers: `BlobStorageErrorMapper` and `QueueStorageErrorMapper`, 
    adapting with a strategy pattern to enhance maintainability and separation of concerns.
  - == IMPORTANT == It is mandatory to add -skipApiVersionCheck flag to azurite command to avoid build failures in dev environment with older versions of azurite,
    as the latest version of azurite does not yet support the latest Azure Storage features (02/26).

  
  == next steps: container, devops, queue storage, function ? retry policies? => stream seekable ?
dont forget to uncomment integration tests for blob storage and queue storage as they need azurite storage emulator or actual azure storage account to run,
- and they are currently commented out to avoid build failures in environments without azure storage access.
  
      
      azurite --skipApiVersionCheck
