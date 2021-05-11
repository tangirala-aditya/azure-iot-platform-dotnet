import * as React from 'react';
import {TitleBar} from './titleBar';

const pageStyle = {
    width: "100%",
    height: "100%",
};

export const DevicePage = ({ open, children }) => {
    
    return(
    <div style={pageStyle}>
        <TitleBar />
    </div>);
}