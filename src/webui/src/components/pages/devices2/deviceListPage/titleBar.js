//import * as React from 'react';
import { Stack } from '@fluentui/react/lib/Stack';
  // Note: There is likely a much better way to adress graphics.  We don't seem to have webpack properply setup so I've default to the approach below.
  // check out icons here: src\webui\node_modules\@fluentui\font-icons-mdl2\lib\IconNames.d.ts
  //  and src\webui\azure-iot-ux-fluent-controls\lib\common\_icons.scss


export const TitleBar = () => {
 
  const imageContainerStyle = {
    marginLeft: "25px", width:"65px",height:"60px",borderRadius:"50%", border:"1px solid #9e9e9e", borderColor:"#9e9e9e",margin:"10px",backgroundColor:"#f9f7f7", 
  }
  const imageStyle = {
    fontSize:"36px",marginTop:"3px",marginLeft:"12px"
  }
  const labelStyle = {
    fontSize:"24px",fontWeight:"500",marginTop:"25px",marginLeft:"5px", width:"100%"
  }
  
  return (
    <Stack horizontal>
      <div style={imageContainerStyle}>
        <div className="icon icon-dialShape3" style={imageStyle}></div>
      </div>
      <div style={labelStyle}>All Devices</div>
    </Stack>
   
  );

}

/*

 <div style={imageContainerStyle}>
    
    <span style={labelStyle}>All Devices</span>
    </div>
  return (
    <Stack horizontal>

    </Stack>
    <div style={imageContainerStyle}>
    <div className="icon icon-dialShape3" style={imageStyle}></div>
    <span style={labelStyle}>All Devices</span>
    </div>
  );
<Image
      src={pic}
      alt='Example of the image fit value "centerCover" on an image smaller than the frame.'
      />
      {...imageProps}



<ReactSVG src="device.svg" />
<Svg
          src={svgs.tabs.devices2}
          className={css("global-nav-item-icon")}
          style={{width: "60px",height:"30px", color:"black"}} 
      />

      < 
*/