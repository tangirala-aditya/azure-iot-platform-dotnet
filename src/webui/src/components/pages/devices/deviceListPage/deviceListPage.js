import * as React from 'react';
import {TitleBar} from './titleBar';
import {Toolbar} from './toolbar';
import {DeviceList} from './deviceList';


//export const DeviceListPage = ({ open, children }) => {

export class DeviceListPage extends React.Component {    

    render() {

        return(
            <div style={{width: "100%", height: "100%"}}>
                <TitleBar {...this.props} />
                <Toolbar {...this.props} />
                <DeviceList {...this.props} onOpenDevicePage={this.props.onOpenDevicePage} />
            </div>
        );
    }
    
}