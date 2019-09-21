---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
- services: virtual-network,network-watcher
- platforms: dotnet
description: "Azure Network sample for enabling and updating network peering between two virtual networks."
---

# Use network watcher to check connectivity between virtual machines in peered networks #

 Azure Network sample for enabling and updating network peering between two virtual networks
 Summary ...
 - This sample uses Azure Network Watcher's connectivity check to verify connectivity between
   two peered virtual networks.
 Details ...
 1. Define two virtual networks network "A" and network "B" with one subnet each
 2. Create two virtual machines, each within a separate network
   - The virtual machines currently must use a special extension to support Network Watcher
 3. Peer the networks...
   - the peering will initially have default settings:
   - each network's IP address spaces will be accessible from the other network
   - no traffic forwarding will be enabled between the networks
   - no gateway transit between one network and the other will be enabled
 4. Use Network Watcher to check connectivity between the virtual machines in different peering scenarios:
   - both virtual machines accessible to each other (bi-directional)
   - virtual machine A accessible to virtual machine B, but not the other way


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/network-dotnet-use-network-watcher-to-check-connectivity.git

    cd network-dotnet-use-network-watcher-to-check-connectivity

    dotnet build

    bin\Debug\net452\VerifyNetworkPeeringWithNetworkWatcher.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
