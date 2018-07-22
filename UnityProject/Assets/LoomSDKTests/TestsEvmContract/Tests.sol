pragma solidity ^0.4.24;

contract Tests {
    event TestEvent(uint number);

    address testAddress;
    int testInt;
    uint testUint;
    bytes testByteArray;
    bytes4 testFixed4ByteArray;
    bytes32 testFixed32ByteArray;
    bytes testStaticByteArray;

    constructor() public {
        testStaticByteArray.push(1);
        testStaticByteArray.push(2);
        testStaticByteArray.push(3);
        testStaticByteArray.push(4);
    }

    function emitTestEvent(uint number) public {
        emit TestEvent(number);
    }

    // events
    function emitTestEvents(uint base) public {
        emit TestEvent(base + 1);
        emit TestEvent(base + 2);
        emit TestEvent(base + 3);
        emit TestEvent(base + 4);
        emit TestEvent(base + 5);
        emit TestEvent(base + 6);
        emit TestEvent(base + 7);
        emit TestEvent(base + 8);
        emit TestEvent(base + 9);
        emit TestEvent(base + 10);
        emit TestEvent(base + 11);
        emit TestEvent(base + 12);
        emit TestEvent(base + 13);
        emit TestEvent(base + 14);
        emit TestEvent(base + 15);
    }

    // address

    function setTestAddress(address _testAddress) public {
    	testAddress = _testAddress;
    }

    function getTestAddress() public view returns (address) {
    	return testAddress;
    }

    function getStaticTestAddress() public pure returns (address) {
        return 0x1D655354f10499ef1E32e5a4e8B712606AF33628;
    }

    // uint

    function setTestUint(uint _testUint) public  {
    	testUint = _testUint;
    }

    function getTestUint() public view returns (uint) {
    	return testUint;
    }

    function getStaticTestUint() public pure returns (uint) {
        return 0xDEADBEEF;
    }

    // int

    function setTestInt(int _testInt) public {
    	testInt = _testInt;
    }

    function getTestInt() public view returns (int) {
    	return testInt;
    }

    function getStaticTestIntPositive() public pure returns (int) {
        return 0xDEADBEEF;
    }

    function getStaticTestIntNegative() public pure returns (int) {
        return -0xDEADBEEF;
    }

    function getStaticTestIntMinus1() public pure returns (int) {
        return -1;
    }

    function getStaticTestIntMinus255() public pure returns (int) {
        return -255;
    }

    function getStaticTestIntMinus256() public pure returns (int) {
        return -256;
    }

    // bytes

    function setTestByteArray(bytes _testByteArray) public {
    	testByteArray = _testByteArray;
    }

    function getTestByteArray() public view returns (bytes) {
    	return testByteArray;
    }

    function getStaticTestByteArray() public view returns (bytes) {
        return testStaticByteArray;
    }

    // bytes4

    function setTestFixed4ByteArray(bytes4 _testFixed4ByteArray) public {
    	testFixed4ByteArray = _testFixed4ByteArray;
    }

    function getTestFixed4ByteArray() public view returns (bytes4) {
    	return testFixed4ByteArray;
    }

    function getStaticTestFixed4ByteArray() public pure returns (bytes4) {
        return 0xDEADBEEF;
    }

    // bytes32

    function setTestFixed32ByteArray(bytes32 _testFixed32ByteArray) public {
    	testFixed32ByteArray = _testFixed32ByteArray;
    }

    function getTestFixed32ByteArray() public view returns (bytes32) {
    	return testFixed32ByteArray;
    }

    function getStaticTestFixed32ByteArray() public pure returns (bytes32) {
        return 0xDEADBEEF;
    }
}