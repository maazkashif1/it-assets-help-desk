using System.ServiceModel;

namespace ItAssetsHelpDesk.Web.Services;

// SOAP endpoint exposed via SoapCore at /soap/assets.svc
[ServiceContract]
public interface IAssetSoapService
{
    [OperationContract]
    AssetSoapDto GetAssetByTag(string assetTag);
}
