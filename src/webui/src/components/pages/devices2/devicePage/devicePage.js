import React from 'react';
import {TitleBar} from './titleBar';



export const DevicePage = ({ open, children }) => {
    
    
    const divStyle = {
        marginTop: 30,
        marginLeft: 40,
    }
    return(
    <div style={divStyle}>
        <TitleBar />
        
    </div>);
}