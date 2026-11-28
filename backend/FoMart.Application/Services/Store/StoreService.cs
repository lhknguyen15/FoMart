using FoMart.Application.DTO;
using FoMart.Application.DTO.Store;
using FoMart.Application.Interfaces.UnitOfWork;
using StoreEntity = FoMart.Domain.Entities.Store;

namespace FoMart.Application.Services.Store;

public sealed class StoreService(IUnitOfWork unitOfWork)
{
    // Lấy thông tin cửa hàng (core.stores, dòng duy nhất).
    public async Task<HTTPResponseData<StoreResponse?>> GetStoreAsync(CancellationToken cancellationToken)
    {
        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        if (store is null)
        {
            return new HTTPResponseData<StoreResponse?>
            {
                DataResponse = null,
                Message = StoreResponseMessageDTO.StoreNotConfigured,
                StatusCode = 500
            };
        }

        return new HTTPResponseData<StoreResponse?>
        {
            DataResponse = Map(store),
            Message = StoreResponseMessageDTO.GetStoreSuccess,
            StatusCode = 200
        };
    }

    // Lấy tài khoản nhận chuyển khoản (bank_bin, bank_account_no, bank_account_name) để vẽ mã VietQR.
    public async Task<HTTPResponseData<StorePaymentResponse?>> GetPaymentAsync(CancellationToken cancellationToken)
    {
        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        if (store is null)
            return HTTPResponse.Fail<StorePaymentResponse>(500, StoreResponseMessageDTO.StoreNotConfigured);
        if (string.IsNullOrWhiteSpace(store.BankBin) || string.IsNullOrWhiteSpace(store.BankAccountNo) || string.IsNullOrWhiteSpace(store.BankAccountName))
            return HTTPResponse.Fail<StorePaymentResponse>(404, StoreResponseMessageDTO.PaymentNotConfigured);

        return HTTPResponse.Success(
            new StorePaymentResponse(store.BankBin.Trim(), store.BankAccountNo.Trim(), store.BankAccountName.Trim()),
            StoreResponseMessageDTO.GetPaymentSuccess);
    }

    private static StoreResponse Map(StoreEntity store) =>
        new(store.Name, store.Address, store.Phone, store.BankAccountNo is not null);
}
