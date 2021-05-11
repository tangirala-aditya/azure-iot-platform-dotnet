import * as React from 'react';
import {TitleBar} from './titleBar';
import {Toolbar} from './toolbar';
import {DeviceList} from './deviceList';

const pageStyle = {
    width: "100%",
    height: "100%",
};

export const DeviceListPage = ({ open, children }) => {
    
    return(
    <div style={pageStyle}>
        <TitleBar />
        <Toolbar />
        <DeviceList />
        </div>);
}