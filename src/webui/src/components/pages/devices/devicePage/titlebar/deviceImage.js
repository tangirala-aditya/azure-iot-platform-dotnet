import * as React from 'react';

const imageContainerStyle = {
    margin:"10px 20px 10px 20px",
    width:"80px",height:"80px",borderRadius:"50%",
    border:"1px solid #9e9e9e",
    borderColor:"#9e9e9e",backgroundColor:"#f9f7f7", 
  }
  const imageStyle = {
    fontSize:"48px",marginTop:"3px",marginLeft:"17px"
  }

export class DeviceImage extends React.Component {

    render(){
        return (
            <span style={imageContainerStyle}>
                <div className="icon icon-dialShape3" style={imageStyle}></div>
            </span>
        )   
    }
}