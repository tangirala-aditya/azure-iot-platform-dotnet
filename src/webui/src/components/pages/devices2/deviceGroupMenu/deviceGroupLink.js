import * as React from "react";
import "./deviceGroupLink.css";

export const DeviceGroupLink = ({ name, select }) => {

    
    //const [isHover, setIsHovering] = React.useState(selected);
    //<div className="deviceGroupItemHover"  onMouseEnter={() => setIsHovering(true)} onMouseLeave={() => setIsHovering(false)}>

    return (
        <div className={"deviceGroupItemHover" + (select? ' Selected': '')}  >
            <span style={{padding:"16px", lineHeight:"35px"}}>
            {name}
            </span>
        </div>
    )
}