import * as React from 'react';
import { Stack } from '@fluentui/react/lib/Stack';
import iconPath from "assets/icons/simulatedDevice.svg";
//import DeviceGroupIconPath from "assets/icons/deviceGroup.svg";

  // Note: There is likely a much better way to adress graphics.  We don't seem to have webpack properply setup so I've default to the approach below.
  // check out icons here: src\webui\node_modules\@fluentui\font-icons-mdl2\lib\IconNames.d.ts
  //  and src\webui\azure-iot-ux-fluent-controls\lib\common\_icons.scss

/* 
  Check out other options for SVGs:
  https://www.npmjs.com/package/@fluentui/svg-icons
  https://github.com/microsoft/fluentui-system-icons#readme
  https://github.com/microsoft/fluentui-system-icons/blob/master/icons.md

*/

const imageContainerStyle = {
  margin:"10px", marginLeft: "10px", width:"65px",height:"60px",borderRadius:"50%", border:"1px solid #9e9e9e", borderColor:"#9e9e9e",backgroundColor:"#f9f7f7", 
}
const imageStyle = {
  fontSize:"30px",marginTop:"8px",marginLeft:"14px"
}
const labelStyle = {
  fontSize:"24px",fontWeight:"500",marginTop:"25px",marginLeft:"5px", width:"100%"
}


export class TitleBar extends React.Component {  
 
  render() {
   
    const {currentDeviceGroup} = this.props;

    //const iconPath = import(currentDeviceGroup.icon); // see technique: https://stackoverflow.com/questions/45746881/reactjs-import-files-within-render

    return (
      <Stack horizontal>
        <div style={imageContainerStyle}>
          <div className="icon " style={imageStyle}>
            <img src={iconPath} style={{width:30, height:30}} alt="Device Icon"/>
          </div>
        </div>
        <div style={labelStyle}>{currentDeviceGroup.name}</div>
      </Stack>
    );
  }
  
}
