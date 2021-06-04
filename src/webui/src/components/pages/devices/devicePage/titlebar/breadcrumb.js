import * as React from 'react';
import {Flex} from "./flex";


export class Breadcrumb extends React.Component {

    render(){
        return (
            <Flex container  padding="10px 0" width="100%">
                <div>Content on Left</div>
                <div>Content on Right</div>
            </Flex> 
        )   
    }
}