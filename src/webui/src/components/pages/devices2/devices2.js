// Copyright (c) 3M. All rights reserved.
import React, { Component } from "react";
import {DeviceGroupMenu} from "./deviceGroupMenu/deviceGroupMenu"
import {DeviceListPage} from "./deviceListPage/deviceListPage"
import {DevicePage} from "./devicePage/devicePage";
import { Stack, } from '@fluentui/react/lib/Stack';


const DevicePageView = {
    deviceList: 'deviceList',
    device: 'device'
}

export class Devices2 extends Component {
    constructor(props) {
        super(props);
        this.state = {
            contextBtns: null,
            view: DevicePageView.device
        };
    }
    
    
    render() {

        const pageStyle = {
            width: "100%",
            height: "100%"
        };
    
        if(this.state.view === DevicePageView.device){
            return (
                <DevicePage />                
            );
        }else {
            return (
                <Stack horizontal style={pageStyle}>
                    <DeviceGroupMenu />
                    <DeviceListPage />
                </Stack>
            );
        }
        
    } 
}