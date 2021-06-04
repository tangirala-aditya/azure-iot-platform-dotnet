import * as React from 'react';
import {Flex} from "./flex";
import {Breadcrumb} from "./breadcrumb";
import {DeviceTitle} from "./title";

export class TitleContent extends React.Component {

    render(){
        return (
            <Flex flex={1} width="100%">
                <Breadcrumb />
                <DeviceTitle  />
                <Flex container padding="0" width="100%">
                    <div>Content on Left</div>
                    <div>Content on Right</div>
                </Flex>
            </Flex>
        )   
    }
}