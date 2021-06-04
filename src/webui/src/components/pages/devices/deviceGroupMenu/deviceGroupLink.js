import * as React from "react";
import "./deviceGroupLink.css";

export const DeviceGroupLink = ({ deviceGroup, isCurrent, onChange }) => {

    
    //const [isHover, setIsHovering] = React.useState(selected);
    //<div className="deviceGroupItemHover"  onMouseEnter={() => setIsHovering(true)} onMouseLeave={() => setIsHovering(false)}>

    
    return (
        <div className={"deviceGroupItemHover" + (isCurrent? ' Selected': '')} onClick={() => onChange(deviceGroup)} >
            <span style={{padding:"16px", lineHeight:"35px"}}>
            {deviceGroup.name}
            </span>
        </div>
    )
}