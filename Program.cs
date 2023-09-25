// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;

namespace VerifyNetworkPeeringWithNetworkWatcher
{
    public class Program
    {
        /**
         * Azure Network sample for enabling and updating network peering between two virtual networks
         *
         * Summary ...
         *
         * - This sample uses Azure Network Watcher's connectivity check to verify connectivity between
         *   two peered virtual networks.
         *
         * Details ...
         *
         * 1. Define two virtual networks network "A" and network "B" with one subnet each
         *
         * 2. Create two virtual machines, each within a separate network
         *   - The virtual machines currently must use a special extension to support Network Watcher

         * 3. Peer the networks...
         *   - the peering will initially have default settings:
         *   - each network's IP address spaces will be accessible from the other network
         *   - no traffic forwarding will be enabled between the networks
         *   - no gateway transit between one network and the other will be enabled
         *
         * 4. Use Network Watcher to check connectivity between the virtual machines in different peering scenarios:
         *   - both virtual machines accessible to each other (bi-directional)
         *   - virtual machine A accessible to virtual machine B, but not the other way
         */
        private static ResourceIdentifier? _resourceGroupId = null;

        public static async Task RunSample(ArmClient client)
        {
            string rgName = Utilities.CreateRandomName("NetworkSampleRG");
            string vnetAName = Utilities.CreateRandomName("netA-");
            string vnetBName = Utilities.CreateRandomName("netB-");
            string vmName1 = Utilities.CreateRandomName("vm1-");
            string vmName2 = Utilities.CreateRandomName("vm2-");
            string networkWatcherName = Utilities.CreateRandomName("netwch");
            string peeringABName = Utilities.CreateRandomName("peer");
            string[] vmIPAddresses = new String[] {
                /* within subnetA */ "10.0.0.8",
                /* within subnetB */ "10.1.0.8"
            };


            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                Utilities.Log($"Creating resource group...");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //=============================================================
                // Define two virtual networks to peer and put the virtual machines in, at specific IP addresses

                Utilities.Log("Creating a new virtual network...");
                VirtualNetworkData vnetInput1 = new VirtualNetworkData()
                {
                    Location = resourceGroup.Data.Location,
                    AddressPrefixes = { "10.0.0.0/27" },
                    Subnets =
                    {
                        new SubnetData() { AddressPrefix = "10.0.0.0/27", Name = "subnetA" },
                    },
                };
                var vnetLro1 = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, vnetAName, vnetInput1);
                VirtualNetworkResource vnet1 = vnetLro1.Value;
                Utilities.Log($"Created a virtual network: {vnet1.Data.Name}");

                Utilities.Log("Creating a new virtual network...");
                VirtualNetworkData vnetInput2 = new VirtualNetworkData()
                {
                    Location = resourceGroup.Data.Location,
                    AddressPrefixes = { "10.1.0.0/27" },
                    Subnets =
                    {
                        new SubnetData() { AddressPrefix = "10.1.0.0/27", Name = "subnetB" },
                    },
                };
                var vnetLro2 = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, vnetBName, vnetInput2);
                VirtualNetworkResource vnet2 = vnetLro2.Value;
                Utilities.Log($"Created a virtual network: {vnet2.Data.Name}");

                //=============================================================
                // Define a couple of Windows VMs and place them in each of the networks

                // Definate vm extension input data
                string extensionName = "AzureNetworkWatcherExtension";
                var extensionInput = new VirtualMachineExtensionData(resourceGroup.Data.Location)
                {
                    Publisher = "Microsoft.Azure.NetworkWatcher",
                    ExtensionType = "NetworkWatcherAgentWindows",
                    TypeHandlerVersion = "1.4",
                    AutoUpgradeMinorVersion = true,
                };

                // Create vm1
                Utilities.Log("Creating a new virtual machine...");
                NetworkInterfaceResource nic1 = await Utilities.CreateNetworkInterface(resourceGroup, vnet1);
                VirtualMachineData vmInput1 = Utilities.GetDefaultVMInputData(resourceGroup, vmName1);
                vmInput1.NetworkProfile.NetworkInterfaces.Add(new VirtualMachineNetworkInterfaceReference() { Id = nic1.Id, Primary = true });
                var vmLro1 = await resourceGroup.GetVirtualMachines().CreateOrUpdateAsync(WaitUntil.Completed, vmName1, vmInput1);
                VirtualMachineResource vm1 = vmLro1.Value;
                _ = await vm1.GetVirtualMachineExtensions().CreateOrUpdateAsync(WaitUntil.Completed, extensionName, extensionInput);
                Utilities.Log($"Created vm: {vm1.Data.Name}");

                // Create vm2
                Utilities.Log("Creating a new virtual machine...");
                NetworkInterfaceResource nic2 = await Utilities.CreateNetworkInterface(resourceGroup, vnet2);
                VirtualMachineData vmInput2 = Utilities.GetDefaultVMInputData(resourceGroup, vmName2);
                vmInput2.NetworkProfile.NetworkInterfaces.Add(new VirtualMachineNetworkInterfaceReference() { Id = nic2.Id, Primary = true });
                var vmLro2 = await resourceGroup.GetVirtualMachines().CreateOrUpdateAsync(WaitUntil.Completed, vmName2, vmInput2);
                VirtualMachineResource vm2 = vmLro2.Value;
                _ = await vm2.GetVirtualMachineExtensions().CreateOrUpdateAsync(WaitUntil.Completed, extensionName, extensionInput);
                Utilities.Log($"Created vm: {vm2.Data.Name}");

                //=============================================================
                // Peer the two networks using default settings

                Utilities.Log(
                        "Peering the networks using default settings...\n"
                        + "- Network access enabled\n"
                        + "- Traffic forwarding disabled\n"
                        + "- Gateway use (transit) by the remote network disabled");

                Utilities.Log("Creating peering between vnetA and vnetB...");
                VirtualNetworkPeeringData peeringBaseInput = new VirtualNetworkPeeringData()
                {
                    AllowVirtualNetworkAccess = true,
                    AllowForwardedTraffic = true,
                    AllowGatewayTransit = false,
                    UseRemoteGateways = false,
                    RemoteVirtualNetworkId = vnet2.Id,
                };
                // Create a peering at vnet1
                VirtualNetworkPeeringData peeringInput1 = peeringBaseInput;
                peeringInput1.RemoteVirtualNetworkId = vnet2.Id;
                _ = await vnet1.GetVirtualNetworkPeerings().CreateOrUpdateAsync(WaitUntil.Completed, peeringABName, peeringInput1);
                // Create a peering at vnet2 to establish a synchronized connection.
                VirtualNetworkPeeringData peeringInput2 = peeringBaseInput;
                peeringInput1.RemoteVirtualNetworkId = vnet1.Id;
                _ = await vnet2.GetVirtualNetworkPeerings().CreateOrUpdateAsync(WaitUntil.Completed, peeringABName, peeringInput1);
                // Get peering
                var peeringLro = await vnet1.GetVirtualNetworkPeerings().GetAsync(peeringABName);
                VirtualNetworkPeeringResource peering = peeringLro.Value;
                Utilities.Log($"Created peering: {peering.Data.Name}");

                //=============================================================
                // Check connectivity between the two VMs/networks using Network Watcher
                Utilities.Log($"Create a network watcher in {resourceGroup.Data.Location}...");
                Utilities.Log("To note: one subscription only has a Network Watcher in the same region");
                Utilities.Log($"         make sure there has not Network Watcher in {resourceGroup.Data.Location}");
                NetworkWatcherData networkWatcherInput = new NetworkWatcherData()
                {
                    Location = resourceGroup.Data.Location,
                };
                //var networkWatcherLro = await resourceGroup.GetNetworkWatchers().CreateOrUpdateAsync(WaitUntil.Completed, networkWatcherName, networkWatcherInput);
                //NetworkWatcherResource networkWatcher = networkWatcherLro.Value;
                var watcherRG = await subscription.GetResourceGroups().GetAsync("NetworkWatcherRG");
                var networkWatcherLro = await watcherRG.Value.GetNetworkWatchers().GetAsync("NetworkWatcher_eastus");
                NetworkWatcherResource networkWatcher = networkWatcherLro.Value;

                // Verify bi-directional connectivity between the VMs on port 22 (SSH enabled by default on Linux VMs)
                // Block: waiting issue https://github.com/Azure/azure-sdk-for-net/pull/38876 fix
                ConnectivityContent contentA2B = new ConnectivityContent(
                   new ConnectivitySource(vm1.Id),
                   new ConnectivityDestination() { Port = 22, ResourceId = vm2.Id });
                var connectivityA2BResult = await networkWatcher.CheckConnectivityAsync(WaitUntil.Completed, contentA2B);
                Utilities.Log("Connectivity from A to B: " + connectivityA2BResult.Value.NetworkConnectionStatus);

                ConnectivityContent contentB2A = new ConnectivityContent(
                  new ConnectivitySource(vm2.Id),
                  new ConnectivityDestination() { Port = 22, ResourceId = vm1.Id });
                var connectivityB2AResult = await networkWatcher.CheckConnectivityAsync(WaitUntil.Completed, contentB2A);
                Utilities.Log("Connectivity from B to A: " + connectivityB2AResult.Value.NetworkConnectionStatus);

                // Change the peering to allow access between A and B
                Utilities.Log("Changing the peering to disable access between A and B...");
                VirtualNetworkPeeringData peeringUpdateInput = new VirtualNetworkPeeringData()
                {
                    AllowVirtualNetworkAccess = false,
                };
                var updatePeeringLro = await peering.UpdateAsync(WaitUntil.Completed, peeringUpdateInput);
                VirtualNetworkPeeringResource updatedPeering = updatePeeringLro.Value;

                // Verify connectivity no longer possible between A and B
                Utilities.Log("Peering configuration changed.\nNow, A should be unreachable from B, and B should be unreachable from A...");
                Utilities.Log("Connectivity from A to B: " + connectivityA2BResult.Value.NetworkConnectionStatus);
                Utilities.Log("Connectivity from B to A: " + connectivityB2AResult.Value.NetworkConnectionStatus);
            }
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group...");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId.Name}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
            ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            ArmClient client = new ArmClient(credential, subscription);

            await RunSample(client);

            try
            {
                //=================================================================
                // Authenticate
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}