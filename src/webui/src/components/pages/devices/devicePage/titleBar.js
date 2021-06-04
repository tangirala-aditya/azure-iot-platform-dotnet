import * as React from 'react';
import { Stack } from '@fluentui/react/lib/Stack';
//import { Breadcrumb } from '@fluentui/react/lib/Breadcrumb';
import { Label, Pivot, PivotItem } from '@fluentui/react';

  // Note: There is likely a much better way to address graphics.  We don't seem to have webpack properply setup so I've default to the approach below.
  // check out icons here: src\webui\node_modules\@fluentui\font-icons-mdl2\lib\IconNames.d.ts
  // and src\webui\azure-iot-ux-fluent-controls\lib\common\_icons.scss

  const imageContainerStyle = {
    marginLeft: "25px", width:"65px",height:"60px",borderRadius:"50%", border:"1px solid #9e9e9e", borderColor:"#9e9e9e",margin:"10px",backgroundColor:"#f9f7f7", 
  }
  const imageStyle = {
    fontSize:"36px",marginTop:"3px",marginLeft:"15px"
  }
  const labelStyle = {
    fontSize:"24px",fontWeight:"500",marginLeft:"5px", width:"100%"
  }
  
  const labelStyles = {
    root: { marginTop: 10 },
  };

  
  const navStyles = {
    root: {      
      color: 'rgb(50, 49, 48)',
      display: 'flex',
      fontSize: '12px',
      fontWeight: 400
    },
  };

  const stackTokens = {
    childrenGap: 5,
    padding: 10,
  };


export class TitleBar extends React.Component {

 
  render() {


  return (
    <>
    <Stack horizontal>
      <div style={imageContainerStyle}>
        <div className="icon icon-dialShape3" style={imageStyle}></div>
      </div>
      <div style={{width:"100%"}}>
          <div style={{wdith:"100%"}}>
            <Stack horizontal tokens={stackTokens}>
              <Stack.Item align="stretch" styles={navStyles}>
                <span>Get values from above</span>
              </Stack.Item>
              <Stack.Item align="end" styles={navStyles}>
                <span style={{width:"100%",textAlign:"right"}}>Test</span></Stack.Item>
              </Stack>
            <div style={labelStyle}>HoboMx-100-v2-X</div>
          </div>
      </div>
      
    </Stack>
  
    <Pivot aria-label="Basic Pivot Example" style={{marginLeft:"82px",marginTop:"-15px"}}>
    <PivotItem
      headerText="Device Data"
      headerButtonProps={{
        'data-order': 1,
        'data-title': 'My Files Title',
      }}
    >
      <Label styles={labelStyles}>Pivot #1: Content Goes Here</Label>
    </PivotItem>
    <PivotItem headerText="Device Info">
      <Label styles={labelStyles}>Pivot #2: Content Goes Here</Label>
    </PivotItem>
    </Pivot>

   </>
  );
    }
}


/*
  const items = [
    { text: 'Devices', key: 'Devices', as: ''},
    { text: 'HoboMX', key: 'HoboMX', isCurrentItem: true, as: '' },
  ];

  
*/
  
  



  /*
   const breadcrumContainerStyle = {
    marginTop: 12,
    marginLeft: 5,
    textDecoration: 'none',
  }
   const breadcrumStyle = {
    color: 'rgb(50, 49, 48)',
    fontSize: '12px',
    fontWeight: 400
  }

    //  <span><a href='#' alt="return to devices groups" style={breadcrumStyle}> Devices</a> </span><span style={{paddingLeft:4, paddingRight: 4}}> \ </span><span><a style={breadcrumStyle} href='#' alt="return to device list"> Hobo Mx </a> </span>      

    
  */



/*
<Breadcrumb
          items={items}
          maxDisplayedItems={10}
          ariaLabel="Breadcrumb with items rendered as buttons"
          overflowAriaLabel="More links"        
        />
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