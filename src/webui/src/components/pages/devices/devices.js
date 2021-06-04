// Copyright (c) 3M. All rights reserved.
import React from "react";
import {DeviceGroupMenu} from "./deviceGroupMenu/deviceGroupMenu"
import {DeviceListPage} from "./deviceListPage/deviceListPage"
import {DevicePage} from "./devicePage/devicePage";
import { Stack, } from '@fluentui/react/lib/Stack';
import DataDeviceGroups from './cache/deviceGroups';
import DataDevices from './cache/devices';
import DataGroupedDevices from './cache/groupedDevices';


export class Devices extends React.Component {

    

    constructor(props) {
        super(props);

        const currentDeviceGroup = DataDeviceGroups[0];  // TODO: handle null and empty        
        var currentDevices = this.getDeviceMatch(currentDeviceGroup);

        this.state = {
            isOpenDeviceGroup: true,
            deviceGroups: DataDeviceGroups,
            currentDeviceGroup: currentDeviceGroup,            
            devices: currentDevices,
            currentDevice: null,
            //...closedFlyoutState,
            //contextBtns: null,
            //selectedDeviceGroupId: undefined,
            //loadMore: props.loadMoreState,
            //isDeviceSearch: false,
        };

        this.props.updateCurrentWindow("Devices");
    }

    getDeviceMatch(deviceGroup) {     

        const devicesInGroup = DataGroupedDevices[deviceGroup.id];    

        return DataDevices.filter(function(device){
            return devicesInGroup.devices.some(function(deviceId){
                return device.deviceId === deviceId;                
            })
        })      
    }

    onDeviceGroupChange = (deviceGroup) => {             
        
        var currentDevices = this.getDeviceMatch(deviceGroup);
        this.setState({currentDeviceGroup: deviceGroup, devices: currentDevices});
        // great article on state and spreading: https://blog.logrocket.com/a-guide-to-usestate-in-react-ecb9952e406c/#howtoupdatestateinanestedobjectinreactwithhooks        
    }

    onOpenDevicePage = (deviceId) => {        
        const device = DataDevices.find(d => {return d.deviceId === deviceId});  
        this.setState({currentDevice: device})
    }

    onCloseDevicePage = () => {

    }

    render() {

        if(this.state.currentDevice){
            return (<DevicePage {...this.state} onClose={this.onCloseDevicePage} />);
        }

        return (
                <Stack horizontal style={{width: "100%", height: "100%"}}>
                <DeviceGroupMenu {...this.state} onChange={this.onDeviceGroupChange} />
                <DeviceListPage {...this.state} onOpenDevicePage={this.onOpenDevicePage} />
            </Stack>
        );
    }

}
export default Devices;