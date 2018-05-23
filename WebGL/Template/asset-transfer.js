import Web3 from 'web3';

import transferContractJSON from './RewardTransferContract.json';

/*
// The asset type will be specified by the game, this is just an example...
interface AssetType {
  to: string; // Ethereum address to transfer the asset to
  hash: string; // Asset data
}
*/

async function transferAsset(asset /* AssetType */) {
  console.log('Received request to transfer asset: ', asset);
  if (typeof web3 !== 'undefined') {
    const web3ext = new Web3Extension();
    const account = await web3ext.getSelectedAccount();
    if (!account) {
      throw new Error('No account selected in MetaMask');
    }
    const contract = await web3ext.getContract(transferContractJSON);
    // placeholders for testing
    const UID = 15555;
    const hash = '0xbdbd7b92b8408b4f92764ec5e84e6733f7e6a9ec57b92ee7b7b3037763a1088f'
    const signature = '0x4869f9ec21a3ded6a5d477a1a6c82fc5f791deebd5bda214dc729373de6359ec414d7c6f60936bd8319b186f7bc47898a9a819352a28a198b7d8fe1f695022aa1b'
    const tier = 1;
    const nonce = 0;
    var r = signature.slice(0, 66)
    var s = '0x' + signature.slice(66, 130)
    var v = '0x' + signature.slice(130, 132)
    v = Web3.utils.hexToNumber(v)
    const tx = await contract.methods
      //.transferAsset(asset.to, asset.hash)
      .requestRewards(UID, tier, nonce, r, s, v, hash)
      .send({ gas: '20000', from: account });
    return tx;
  }
  throw new Error('MetaMask not detected');
}

window.LOOM_SETTINGS.transferAsset = transferAsset;

class Web3Extension {
  constructor() {
    this.web3js = new Web3(web3.currentProvider);
  }

  get web3() {
    return this.web3js;
  }

  async getSelectedAccount() {
    const accts = await this.web3js.eth.getAccounts();
    return (accts.length === 0) ? null : accts[0];
  }

  async getContract(contractJSON) {
    const netId = await this.web3js.eth.net.getId();
    const addr = contractJSON.networks[netId] && contractJSON.networks[netId].address;
    if (!addr) {
      throw new Error(`Contract isn't deployed to network ${netId}`);
    }
    return new this.web3js.eth.Contract(contractJSON.abi, addr);
  }
}
